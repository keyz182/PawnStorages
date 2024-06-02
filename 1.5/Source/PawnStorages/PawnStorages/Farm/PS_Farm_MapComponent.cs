﻿using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PawnStorages.Farm
{
    public class PS_Farm_MapComponent : MapComponent
    {
        public List<CompRefuelable> comps = new List<CompRefuelable>();

        public PS_Farm_MapComponent(Map map) : base(map)
        {
        }
    }
}