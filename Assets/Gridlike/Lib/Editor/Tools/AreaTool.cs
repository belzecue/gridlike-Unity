﻿using UnityEngine;
using System;

// TODO [Tool] Copy or drag an area from some place to another, event between grids
[Serializable]
public class AreaTool : GridTool {

	public override bool UseWindow () {
		return true;
	}
	public override string Name() {
		return "select";
	}
}