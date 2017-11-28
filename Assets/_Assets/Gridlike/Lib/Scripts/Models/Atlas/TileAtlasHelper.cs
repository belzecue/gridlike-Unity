﻿using UnityEngine;
using System;
using System.Collections;

namespace Gridlike {

	[Serializable]
	public class TileAtlasHelper {

		[SerializeField] protected TileAtlas _atlas;

		public void _Inject(TileAtlas atlas) {
			_atlas = atlas;
		}

		public TileInfo GetTile(int id) {
			return _atlas [id];
		}
	}
}