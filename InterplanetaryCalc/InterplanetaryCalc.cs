using System;
using SpaceWarp.API.Mods;
using SpaceWarp.API;
using SpaceWarp;
using SpaceWarp.API.UI.Appbar;
using SpaceWarp.UI;
using SpaceWarp.API.Game;
using SpaceWarp.API.Assets;

using UnityEngine.UI;
using KSP;
using UnityEngine;
using I2.Loc;
using KSP.Game;
using KSP.Sim.impl;
using KSP.Sim;
using BepInEx;
using SpaceWarp.API.UI;
using KSP.UI.Binding;

namespace InterplanetaryCalc
{
    [BepInPlugin("com.github.ABritInSpace.InterplanetaryCalc", "InterplanetaryCalc", "0.1.3")]
    [BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]

    public class InterplanetaryCalcMod : BaseSpaceWarpPlugin
    {
        private static InterplanetaryCalcMod Instance { get; set; }
        private bool drawGUI = false;
        
        private Rect window;
        public override void OnInitialized()
        {
            base.OnInitialized();
            Instance = this;
        }

        void Awake()
        {
            window = new Rect((Screen.width)-400,130,350,50);
            drawGUI = true;
        }
        void Update()
        {

        }
        void Populate(int winId)
        {
            GameInstance game = GameManager.Instance.Game;
            VesselComponent vessel = game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true);

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Active Vehicle:", GUILayout.Width(window.width/2));
            GUILayout.Label(vessel?.Name.ToString(), GUILayout.Width(175));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Target Planet:", GUILayout.Width(window.width / 2));
            //Populate rest of menu if target set
            if (vessel.HasTargetObject && vessel.TargetObject.IsCelestialBody && vessel.TargetObject.Orbit.referenceBody.Name == "Kerbol")
            {
                GUILayout.Label(game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true)?.TargetObject.Name.ToString(), GUILayout.Width(175));
                GUILayout.EndHorizontal();
                GUILayout.Label("", GUILayout.Width(350), GUILayout.Height(1));
                Rect underline = GUILayoutUtility.GetLastRect();
                underline.y += underline.height - 2;
                underline.height = 2;
                GUI.Box(underline, "");
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                GUILayout.BeginHorizontal();
                GUILayout.Label("    - Current Phase Ang: ", GUILayout.Width(200));
                GUILayout.Label(Phase().ToString(), GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("    - Desired Phase Ang: ", GUILayout.Width(200));
                GUILayout.Label(Transfer().ToString(), GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label("    - DeltaV for eject: ", GUILayout.Width(200));
                GUILayout.Label(DeltaV().ToString(), GUILayout.Width(150));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }
            //Ignore and don't populate if no target
            else
            {
                GUILayout.Label("None set", GUILayout.Width(175));
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
            }

            GUI.DragWindow();
        }
        void OnGUI()
        {
            if (drawGUI)
            {
                GameInstance game = GameManager.Instance.Game;
                if (game.GlobalGameState.GetState() == GameState.Map3DView && game.ViewController.GetActiveVehicle(true) != null && game.ViewController.GetActiveVehicle(true).GetSimVessel(true).HasTargetObject)
                {
                    GUI.skin = Skins.ConsoleSkin;
                    window = GUILayout.Window(
                        GUIUtility.GetControlID(FocusType.Passive), 
                        window, 
                        Populate, 
                        "Transfer Calculator", 
                        GUILayout.Width(350), 
                        GUILayout.Height(0)
                    );

                }
            }
        }

        double Phase()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;

            CelestialBodyComponent prev = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;
            CelestialBodyComponent cur = prev;
            while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
            {
                prev = cur.Orbit.referenceBody;
            }
            cur = prev;

            CelestialBodyComponent star = target.CelestialBody.GetRelevantStar();
            Vector3d to = star.coordinateSystem.ToLocalPosition(target.Position);
            Vector3d from = star.coordinateSystem.ToLocalPosition(cur.Position);

            double phase = Vector3d.SignedAngle(to, from, Vector3d.up);
            return Math.Round(phase, 1);
        }

        double Transfer()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent prev = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;
            CelestialBodyComponent cur = prev;
            while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
            {
                prev = cur.Orbit.referenceBody;
            }
            cur = prev;

            IKeplerOrbit targetOrbit = target.Orbit;
            IKeplerOrbit currentOrbit = cur.Orbit;

            double ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
            double time = Mathf.PI * Mathf.Sqrt((float)((ellipseA)*(ellipseA)*(ellipseA))/((float)targetOrbit.referenceBody.Mass * 6.67e-11f));
            double transfer;
            
            transfer = 180 - ((time / targetOrbit.period) * 360);
            if (transfer < -180) { transfer += 360; }
            return Math.Round(transfer, 1);
        }
        double DeltaV()
        {
            GameInstance game = GameManager.Instance.Game;
            SimulationObjectModel target = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().TargetObject;
            CelestialBodyComponent prev = game.ViewController.GetActiveVehicle(true)?.GetSimVessel().Orbit.referenceBody;
            CelestialBodyComponent cur = prev;
            while (cur.Orbit.referenceBody.Name != target.Orbit.referenceBody.Name)
            {
                prev = cur.Orbit.referenceBody;
            }
            cur = prev;

            IKeplerOrbit targetOrbit = target.Orbit;
            IKeplerOrbit currentOrbit = cur.Orbit;

            double sunEject;
            double ellipseA = (targetOrbit.semiMajorAxis + currentOrbit.semiMajorAxis) / 2;
            CelestialBodyComponent star = targetOrbit.referenceBody;

            //if (currentOrbit.semiMajorAxis < targetOrbit.semiMajorAxis)
            //{
                sunEject = Mathf.Sqrt((float)(star.gravParameter)/(float)currentOrbit.semiMajorAxis)*(Mathf.Sqrt((float)targetOrbit.semiMajorAxis/(float)ellipseA)-1);
            //}
            /*
            else
            {
                sunEject = Mathf.Sqrt((float)(star.gravParameter) / (float)targetOrbit.semiMajorAxis) * (1-Mathf.Sqrt((float)currentOrbit.semiMajorAxis / (float)ellipseA));
            }
            */

            VesselComponent ship = game.ViewController.GetActiveVehicle(true)?.GetSimVessel(true);
            double eject = Mathf.Sqrt((2*(float)(cur.gravParameter)*((1/(float)ship.Orbit.radius)-(float)(1/cur.sphereOfInfluence))) + (float)(sunEject*sunEject));
            eject -= ship.Orbit.orbitalSpeed;

            return Math.Round(eject, 1);
        }
    }
}
