namespace EcoCompteur
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Numerics;
    using System.Threading;
    using System.Threading.Tasks;
    using Eco.Core.Controller;
    using Eco.Core.Plugins.Interfaces;
    using Eco.Core.Utils;
    using Eco.Gameplay.Components;
    using Eco.Gameplay.Players;
    using Eco.Shared.Localization;
    using Eco.Shared.Logging;
    using Eco.Shared.Math;
    using Eco.World;
    using Eco.World.Blocks;

    /// <summary>
    /// Compteur de vitesse pour les vehicules. Pendant la conduite, la zone d'aide
    /// du vehicule (ControlHints) affiche une jauge, la vitesse en km/h et le bonus
    /// du sol : rouge sous x1 (vegetation, debris), blanc a x1, bleu au-dessus
    /// (routes). Tout est calcule par le serveur : rien a installer pour les joueurs.
    /// </summary>
    public class CompteurPlugin : IModKitPlugin, IInitializablePlugin, IShutdownablePlugin
    {
        private const int    Segments      = 20;
        private const int    PeriodeMs     = 250;
        private const double Fenetre       = 1.0;   // secondes de trajet pour mesurer la vitesse
        private const float  SautMax       = 60f;   // au-dela : teleportation ou bord du monde, mesure ignoree
        private const string Rouge         = "#FF5A5A";
        private const string Bleu          = "#5AB4FF";
        private const string Blanc         = "#FFFFFF";
        private const string Eteint        = "#FFFFFF40";

        private sealed class Etat
        {
            public string Original;
            public string Affiche;
            public float  Vitesse;
            public float  Pointe;
            public readonly Queue<(DateTime Temps, Vector3 Pos)> Trajet = new();
        }

        private readonly Dictionary<VehicleComponent, Etat> etats = new();
        private Timer minuterie;
        private int enCours;

        public string GetCategory()       => Localizer.DoStr("Mods");
        public override string ToString() => Localizer.DoStr("Speedometer");
        public string GetStatus()         => string.Empty;

        public void Initialize(TimedTask timer)
        {
            this.minuterie = new Timer(_ => this.Tick(), null, 5000, PeriodeMs);
            Log.WriteLine(Localizer.NotLocalizedStr("[Compteur] speedometer started"));
        }

        public Task ShutdownAsync()
        {
            this.minuterie?.Dispose();
            lock (this.etats)
            {
                foreach (var (vehicule, etat) in this.etats) Afficher(vehicule, etat.Original);
                this.etats.Clear();
            }
            return Task.CompletedTask;
        }

        private void Tick()
        {
            // Un passage lent ne doit pas se chevaucher avec le suivant.
            if (Interlocked.Exchange(ref this.enCours, 1) == 1) return;
            try
            {
                lock (this.etats)
                {
                    var conduits = new HashSet<VehicleComponent>();
                    foreach (var user in UserManager.Users.Where(u => u.IsOnline))
                    {
                        try
                        {
                            var player = user.Player;
                            if (player == null || !player.MountManager.IsMounted) continue;
                            var vehicule = player.MountManager.Mount?.Parent?.GetComponent<VehicleComponent>();
                            if (vehicule == null || vehicule.Driver != player || !conduits.Add(vehicule)) continue;
                            this.Mettre(vehicule);
                        }
                        catch (Exception e) { Log.WriteErrorLine(Localizer.NotLocalizedStr("[Compteur] update failed: " + e.Message)); }
                    }

                    // Vehicules quittes par leur conducteur : on rend l'aide d'origine.
                    foreach (var vehicule in this.etats.Keys.Where(v => !conduits.Contains(v)).ToList())
                    {
                        Afficher(vehicule, this.etats[vehicule].Original);
                        this.etats.Remove(vehicule);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref this.enCours, 0);
            }
        }

        private void Mettre(VehicleComponent vehicule)
        {
            var maintenant = DateTime.UtcNow;
            var pos        = vehicule.Parent.Position;
            if (!this.etats.TryGetValue(vehicule, out var etat))
                this.etats[vehicule] = etat = new Etat { Original = vehicule.ControlHints };

            etat.Trajet.Enqueue((maintenant, pos));
            while (etat.Trajet.Count > 2 && (maintenant - etat.Trajet.Peek().Temps).TotalSeconds > Fenetre) etat.Trajet.Dequeue();

            var (t0, p0) = etat.Trajet.Peek();
            var duree    = (maintenant - t0).TotalSeconds;
            var distance = Vector3.Distance(p0, pos);
            if (distance > SautMax) { etat.Trajet.Clear(); etat.Trajet.Enqueue((maintenant, pos)); }
            else if (duree > 0.2)
            {
                var mesure    = (float)(distance / duree);
                etat.Vitesse  = etat.Vitesse * 0.4f + mesure * 0.6f;
                if (etat.Vitesse < 0.15f) etat.Vitesse = 0f;
                etat.Pointe   = Math.Max(etat.Pointe, etat.Vitesse);
            }

            var texte = Jauge(etat, vehicule.MaxSpeed, Multiplicateur(pos));
            if (texte != etat.Affiche)
            {
                Afficher(vehicule, texte);
                etat.Affiche = texte;
            }
        }

        private static void Afficher(VehicleComponent vehicule, string texte)
        {
            vehicule.ControlHints = texte;
            vehicule.Changed(nameof(VehicleComponent.ControlHints));
        }

        /// <summary>Bonus de deplacement du sol sous le vehicule : Road(1.2) pour l'asphalte, MoveEfficiency(0.5) pour la vegetation...</summary>
        private static float Multiplicateur(Vector3 pos)
        {
            var origine = new Vector3i((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y + 0.2f), (int)Math.Floor(pos.Z));
            var lent    = 1f;
            for (var dy = 0; dy >= -3; dy--)
            {
                var bloc = World.GetBlock(origine + new Vector3i(0, dy, 0));
                if (bloc == null || bloc.Is<Empty>()) continue;

                var route = bloc.Get<Road>();
                if (route != null && !route.Unset) return route.Efficiency;

                var efficacite = bloc.Get<MoveEfficiency>();
                var valeur     = efficacite != null && !efficacite.Unset ? efficacite.Efficiency : 1f;
                if (bloc.Is<Solid>()) return Math.Min(lent, valeur);
                lent = Math.Min(lent, valeur);   // plantes et debris traverses par les roues
            }
            return lent;
        }

        private static string Jauge(Etat etat, float vitesseMax, float multiplicateur)
        {
            var echelle = Math.Max(Math.Max(1f, vitesseMax) * 1.2f, etat.Pointe);
            var pleins  = (int)Math.Round(Math.Clamp(etat.Vitesse / echelle, 0f, 1f) * Segments);
            var couleur = multiplicateur < 0.995f ? Rouge : multiplicateur > 1.005f ? Bleu : Blanc;
            var kmh     = (int)Math.Round(etat.Vitesse * 3.6f);
            var bonus   = multiplicateur.ToString("0.0#", CultureInfo.InvariantCulture);

            return $"<color={couleur}>{new string('|', pleins)}</color><color={Eteint}>{new string('|', Segments - pleins)}</color>"
                 + $"  <b>{kmh}</b> km/h  <color={couleur}>x{bonus}</color>";
        }
    }
}
