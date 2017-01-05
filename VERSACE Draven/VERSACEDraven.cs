namespace VERSACEDraven
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    using SharpDX;

    using Color = System.Drawing.Color;

    internal class VERSACEDraven

    {


        public Menu Menu { get; set; }
        public Orbwalking.Orbwalker Orbwalker { get; set; }
        public Obj_AI_Hero Player { get { return ObjectManager.Player; } }
        public float ManaPercent { get { return this.Player.Mana / this.Player.MaxMana * 100; } }

        public Spell Q { get; set; }
        public int QCount { get { return (this.Player.HasBuff("dravenspinning") ? 1 : 0) + (this.Player.HasBuff("dravenspinningleft") ? 1 : 0) + this.QReticles.Count; } }
        public List<QRecticle> QReticles { get; set; }
        private int LastAxeMoveTime { get; set; }

        public Spell W { get; set; }
        public Spell E { get; set; }
        public Spell R { get; set; }




        public void Load()
        {

            this.Q = new Spell(SpellSlot.Q, Orbwalking.GetRealAutoAttackRange(this.Player));
            this.W = new Spell(SpellSlot.W);
            this.E = new Spell(SpellSlot.E, 1050);
            this.R = new Spell(SpellSlot.R);

            this.E.SetSkillshot(0.25f, 130, 1400, false, SkillshotType.SkillshotLine);
            this.R.SetSkillshot(0.4f, 160, 2000, true, SkillshotType.SkillshotLine);

            this.QReticles = new List<QRecticle>();

            this.CreateMenu();

            GameObject.OnCreate += this.GameObjectOnOnCreate;
            GameObject.OnDelete += this.GameObjectOnOnDelete;
            AntiGapcloser.OnEnemyGapcloser += this.AntiGapcloserOnOnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += this.Interrupter2OnOnInterruptableTarget;
            Drawing.OnDraw += this.DrawingOnOnDraw;
            Game.OnUpdate += this.GameOnOnUpdate;

        }

        private void CreateMenu()

        {

            this.Menu = new Menu("VERSACE Draven", "versacemenu", true).SetFontStyle(FontStyle.Bold, SharpDX.Color.Gold);

            var combomenu = new Menu("Combo Settings", "combomenu").SetFontStyle(FontStyle.Bold, SharpDX.Color.Red);
            combomenu.AddItem(new MenuItem("UseQCombo", "Use Q").SetValue(true));
            combomenu.AddItem(new MenuItem("UseWCombo", "Use W").SetValue(true));
            combomenu.AddItem(new MenuItem("UseECombo", "Use E").SetValue(true));
            combomenu.AddItem(new MenuItem("UseRCombo", "Use R").SetValue(true));
            this.Menu.AddSubMenu(combomenu);


            var LaneClearMenu = new Menu("Laneclear Settings", "LaneClearMenu").SetFontStyle(
                FontStyle.Bold,
                SharpDX.Color.Red);
            LaneClearMenu.AddItem(new MenuItem("UseQWaveClear", "Use Q").SetValue(true));
            LaneClearMenu.AddItem(new MenuItem("UseWWaveClear", "Use W").SetValue(true));
            LaneClearMenu.AddItem(new MenuItem("UseEWaveClear", "Use E").SetValue(false));
            LaneClearMenu.AddItem(new MenuItem("WaveClearManaPercent", "Mana Percent").SetValue(new Slider(50)));
            this.Menu.AddSubMenu(LaneClearMenu);

            var versacemenu = new Menu("VERSACE Settings", "versacemenu").SetFontStyle(FontStyle.Bold, SharpDX.Color.Red);
            versacemenu.AddItem(new MenuItem("UseWSetting", "Use W Instantly(When Available)").SetValue(false));
            versacemenu.AddItem(new MenuItem("UseEGapcloser", "Use E on Gapcloser").SetValue(true));
            versacemenu.AddItem(new MenuItem("UseEInterrupt", "Use E to Interrupt").SetValue(true));
            versacemenu.AddItem(new MenuItem("UseWManaPercent", "Use W Mana Percent").SetValue(new Slider(50)));
            versacemenu.AddItem(new MenuItem("UseWSlow", "Use W if Slowed").SetValue(true));
            this.Menu.AddSubMenu(versacemenu);

            var axeMenu = new Menu("Axe Settings", "axeSetting").SetFontStyle(FontStyle.Bold, SharpDX.Color.Red);
            axeMenu.AddItem(
                new MenuItem("AxeMode", "Catch Axe on Mode:").SetValue(
                    new StringList(new[] { "Combo", "Any", "Always" }, 2)));
            axeMenu.AddItem(new MenuItem("CatchAxeRange", "Catch Axe Range").SetValue(new Slider(800, 120, 1500)));
            axeMenu.AddItem(new MenuItem("MaxAxes", "Maximum Axes").SetValue(new Slider(2, 1, 3)));
            axeMenu.AddItem(new MenuItem("UseWForQ", "Use W if Axe too far").SetValue(true));
            axeMenu.AddItem(new MenuItem("DontCatchUnderTurret", "Don't Catch Axe Under Turret").SetValue(true));
            this.Menu.AddSubMenu(axeMenu);

            var orbwalkMenu = new Menu("Orbwalker", "orbwalker").SetFontStyle(
                FontStyle.Regular,
                SharpDX.Color.Turquoise);
            this.Orbwalker = new Orbwalking.Orbwalker(orbwalkMenu);
            this.Menu.AddSubMenu(orbwalkMenu);

            var drawMenu = new Menu("Drawing", "draw").SetFontStyle(FontStyle.Regular, SharpDX.Color.Turquoise);
            drawMenu.AddItem(new MenuItem("DrawE", "Draw E").SetValue(true));
            drawMenu.AddItem(new MenuItem("DrawAxeLocation", "Draw Axe Location").SetValue(true));
            drawMenu.AddItem(new MenuItem("DrawAxeRange", "Draw Axe Catch Range").SetValue(true));
            this.Menu.AddSubMenu(drawMenu);

            this.Menu.AddToMainMenu();
        }

        private void GameObjectOnOnCreate(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            this.QReticles.Add(new QRecticle(sender, Environment.TickCount + 1800));
            Utility.DelayAction.Add(1800, () => this.QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId));
        }



        private void GameObjectOnOnDelete(GameObject sender, EventArgs args)
        {
            if (!sender.Name.Contains("Draven_Base_Q_reticle_self.troy"))
            {
                return;
            }

            this.QReticles.RemoveAll(x => x.Object.NetworkId == sender.NetworkId);
        }



        private void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (!this.Menu.Item("UseEGapcloser").IsActive() || !this.E.IsReady()
                || !gapcloser.Sender.IsValidTarget(this.E.Range))
            {
                return;
            }

            this.E.Cast(gapcloser.Sender);
        }



        private void Interrupter2OnOnInterruptableTarget(
    Obj_AI_Hero sender,
    Interrupter2.InterruptableTargetEventArgs args)
        {
            if (!this.Menu.Item("UseEInterrupt").IsActive() || !this.E.IsReady() || !sender.IsValidTarget(this.E.Range))
            {
                return;
            }

            if (args.DangerLevel == Interrupter2.DangerLevel.Medium || args.DangerLevel == Interrupter2.DangerLevel.High)
            {
                this.E.Cast(sender);
            }
        }

        private void DrawingOnOnDraw(EventArgs args)
        {
            var drawE = this.Menu.Item("DrawE").IsActive();
            var drawAxeLocation = this.Menu.Item("DrawAxeLocation").IsActive();
            var drawAxeRange = this.Menu.Item("DrawAxeRange").IsActive();

            if (drawE)
            {
                Render.Circle.DrawCircle(
                    ObjectManager.Player.Position,
                    this.E.Range,
                    this.E.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawAxeLocation)
            {
                var bestAxe =
                    this.QReticles.Where(
                        x =>
                        x.Position.Distance(Game.CursorPos) < this.Menu.Item("CatchAxeRange").GetValue<Slider>().Value)
                        .OrderBy(x => x.Position.Distance(this.Player.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .FirstOrDefault();

                if (bestAxe != null)
                {
                    Render.Circle.DrawCircle(bestAxe.Position, 120, Color.LimeGreen);
                }

                foreach (var axe in
                    this.QReticles.Where(x => x.Object.NetworkId != (bestAxe == null ? 0 : bestAxe.Object.NetworkId)))
                {
                    Render.Circle.DrawCircle(axe.Position, 120, Color.Yellow);
                }
            }

            if (drawAxeRange)
            {
                Render.Circle.DrawCircle(
                    Game.CursorPos,
                    this.Menu.Item("CatchAxeRange").GetValue<Slider>().Value,
                    Color.DodgerBlue);
            }
        }



        private void CatchAxe()
        {
            var catchOption = this.Menu.Item("AxeMode").GetValue<StringList>().SelectedIndex;

            if (((catchOption == 0 && this.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                 || (catchOption == 1 && this.Orbwalker.ActiveMode != Orbwalking.OrbwalkingMode.None))
                || catchOption == 2)
            {
                var bestReticle =
                    this.QReticles.Where(
                        x =>
                        x.Object.Position.Distance(Game.CursorPos)
                        < this.Menu.Item("CatchAxeRange").GetValue<Slider>().Value)
                        .OrderBy(x => x.Position.Distance(this.Player.ServerPosition))
                        .ThenBy(x => x.Position.Distance(Game.CursorPos))
                        .ThenBy(x => x.ExpireTime)
                        .FirstOrDefault();

                if (bestReticle != null && bestReticle.Object.Position.Distance(this.Player.ServerPosition) > 100)
                {
                    var eta = 1000 * (this.Player.Distance(bestReticle.Position) / this.Player.MoveSpeed);
                    var expireTime = bestReticle.ExpireTime - Environment.TickCount;

                    if (eta >= expireTime && this.Menu.Item("UseWForQ").IsActive())
                    {
                        this.W.Cast();
                    }

                    if (this.Menu.Item("DontCatchUnderTurret").IsActive())
                    {
                        // If we're under the turret as well as the axe, catch the axe
                        if (this.Player.UnderTurret(true) && bestReticle.Object.Position.UnderTurret(true))
                        {
                            if (this.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                            {
                                this.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                this.Orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                            }
                        }
                        else if (!bestReticle.Position.UnderTurret(true))
                        {
                            if (this.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                            {
                                this.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                            }
                            else
                            {
                                this.Orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                            }
                        }
                    }
                    else
                    {
                        if (this.Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.None)
                        {
                            this.Player.IssueOrder(GameObjectOrder.MoveTo, bestReticle.Position);
                        }
                        else
                        {
                            this.Orbwalker.SetOrbwalkingPoint(bestReticle.Position);
                        }
                    }
                }
                else
                {
                    this.Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
                }
            }
            else
            {
                this.Orbwalker.SetOrbwalkingPoint(Game.CursorPos);
            }
        }


        private void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
            {
                return;
            }

            this.CatchAxe();
        }


        private void GameOnOnUpdate(EventArgs args)
        {
            this.QReticles.RemoveAll(x => x.Object.IsDead);

            this.CatchAxe();

            if (this.W.IsReady() && this.Menu.Item("UseWSlow").IsActive() && this.Player.HasBuffOfType(BuffType.Slow))
            {
                this.W.Cast();
            }

            switch (this.Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.LaneClear:
                    this.LaneClear();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    this.Combo();
                    break;
            }
        }



        private void Combo()
        {
            var target = TargetSelector.GetTarget(this.E.Range, TargetSelector.DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            var useQ = this.Menu.Item("UseQCombo").IsActive();
            var useW = this.Menu.Item("UseWCombo").IsActive();
            var useE = this.Menu.Item("UseECombo").IsActive();
            var useR = this.Menu.Item("UseRCombo").IsActive();

            if (useQ && this.QCount < this.Menu.Item("MaxAxes").GetValue<Slider>().Value - 1 && this.Q.IsReady()
                && this.Orbwalker.InAutoAttackRange(target) && !this.Player.Spellbook.IsAutoAttacking)
            {
                this.Q.Cast();
            }

            if (useW && this.W.IsReady()
                && this.ManaPercent > this.Menu.Item("UseWManaPercent").GetValue<Slider>().Value)
            {
                if (this.Menu.Item("UseWSetting").IsActive())
                {
                    this.W.Cast();
                }
                else
                {
                    if (!this.Player.HasBuff("dravenfurybuff"))
                    {
                        this.W.Cast();
                    }
                }
            }

            if (useE && this.E.IsReady())
            {
                this.E.Cast(target);
            }

            if (!useR || !this.R.IsReady())
            {
                return;
            }

            var killableTarget =
                HeroManager.Enemies.Where(x => x.IsValidTarget(2000))
                    .FirstOrDefault(
                        x =>
                        this.Player.GetSpellDamage(x, SpellSlot.R) * 2 > x.Health
                        && (!this.Orbwalker.InAutoAttackRange(x) || this.Player.CountEnemiesInRange(this.E.Range) > 2));

            if (killableTarget != null)
            {
                this.R.Cast(killableTarget);
            }
        }



        private void LaneClear()
        {
            var useQ = this.Menu.Item("UseQWaveClear").IsActive();
            var useW = this.Menu.Item("UseWWaveClear").IsActive();
            var useE = this.Menu.Item("UseEWaveClear").IsActive();

            if (this.ManaPercent < this.Menu.Item("WaveClearManaPercent").GetValue<Slider>().Value)
            {
                return;
            }

            if (useQ && this.QCount < this.Menu.Item("MaxAxes").GetValue<Slider>().Value - 1 && this.Q.IsReady()
                && this.Orbwalker.GetTarget() is Obj_AI_Minion && !this.Player.Spellbook.IsAutoAttacking
                && !this.Player.IsWindingUp)
            {
                this.Q.Cast();
            }

            if (useW && this.W.IsReady()
                && this.ManaPercent > this.Menu.Item("UseWManaPercent").GetValue<Slider>().Value)
            {
                if (this.Menu.Item("UseWSetting").IsActive())
                {
                    this.W.Cast();
                }
                else
                {
                    if (!this.Player.HasBuff("dravenfurybuff"))
                    {
                        this.W.Cast();
                    }
                }
            }

            if (!useE || !this.E.IsReady())
            {
                return;
            }

            var bestLocation = this.E.GetLineFarmLocation(MinionManager.GetMinions(this.E.Range));

            if (bestLocation.MinionsHit > 1)
            {
                this.E.Cast(bestLocation.Position);
            }
        }





        internal class QRecticle
        {


            /// <summary>
            ///     Initializes a new instance of the <see cref="QRecticle" /> class.
            /// </summary>
            /// <param name="rectice">The rectice.</param>
            /// <param name="expireTime">The expire time.</param>
            public QRecticle(GameObject rectice, int expireTime)
            {
                this.Object = rectice;
                this.ExpireTime = expireTime;
            }


            public int ExpireTime { get; set; }


            public GameObject Object { get; set; }


            public Vector3 Position
            {
                get
                {
                    return this.Object.Position;
                }
            }


        }
    }
}




