﻿using UnityEngine;
using System.Collections.Generic;

namespace Gridship {

	public abstract class Upgrade {

		public static int upgradeCurrentId = 0;

		public int id { get; private set; }
		public List<int> dependentIds { get; private set; }

		public bool isSpecial;
		public bool isDone;

		protected GSCharacter character { get; private set; }
		protected GSShip ship { get; private set; }

		public Upgrade() {
			id = upgradeCurrentId++;
			dependentIds = new List<int> ();
		}

		public void _Inject(GSCharacter character, GSShip ship) {
			this.character = character;
			this.ship = ship;
		}

		public Upgrade DependOn(Upgrade upgrade) {
			dependentIds.Add (upgrade.id);
			return this;
		}

		public abstract string Name();
		public abstract string Description ();

		public abstract void Execute();
	}
}