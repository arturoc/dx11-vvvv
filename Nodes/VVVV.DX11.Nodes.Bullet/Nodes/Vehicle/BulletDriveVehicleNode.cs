﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VVVV.PluginInterfaces.V2;
using BulletSharp;
using VVVV.Nodes.Bullet;
using VVVV.Utils.VMath;

namespace VVVV.Bullet.Nodes.Bodies.Interactions.Vehicle
{
    [PluginInfo(Name = "Steer", Category = "Bullet", Version="Vehicle", Author = "vux",
        Help = "Drives Bullet Vehicle", AutoEvaluate = true)]
    public class BulletSteerVehicleNode : AbstractBodyInteractionNode<RaycastVehicle>
    {
        [Input("Steering")]
        protected ISpread<float> FSteer;

        [Input("Steering Wheel Index")]
        protected ISpread<int> FSteerWheel;

        protected override void ProcessObject(RaycastVehicle obj, int slice)
        {
            obj.SetSteeringValue(this.FSteer[slice], VMath.Zmod(this.FSteerWheel[slice], obj.NumWheels));
        }
    }

    [PluginInfo(Name = "Brake", Category = "Bullet", Version = "Vehicle", Author = "vux",
    Help = "Drives Bullet Vehicle", AutoEvaluate = true)]
    public class BulletBrakeVehicleNode : AbstractBodyInteractionNode<RaycastVehicle>
    {
        [Input("Brake Force")]
        protected ISpread<float> FBrakeForce;

        [Input("Brake Force Wheel Index")]
        protected ISpread<int> FBrakeForceWheel;

        protected override void ProcessObject(RaycastVehicle obj, int slice)
        {
            obj.SetBrake(this.FBrakeForce[slice], VMath.Zmod(this.FBrakeForceWheel[slice], obj.NumWheels));
        }
    }

    [PluginInfo(Name = "EngineForce", Category = "Bullet", Version = "Vehicle", Author = "vux",
    Help = "Drives Bullet Vehicle", AutoEvaluate = true)]
    public class BulletEngineForceVehicleNode : AbstractBodyInteractionNode<RaycastVehicle>
    {
        [Input("Engine Force")]
        protected ISpread<float> FEngineForce;

        [Input("Engine Force Wheel Index")]
        protected ISpread<int> FEngineForceWheel;

        protected override void ProcessObject(RaycastVehicle obj, int slice)
        {
            obj.ApplyEngineForce(this.FEngineForce[slice], VMath.Zmod(this.FEngineForceWheel[slice], obj.NumWheels));
        }
    }
}
