using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public struct PhysicsFilter
{
	public const uint Default = 1 << 0;
	public const uint Interactable = 1 << 1;
	public const uint Player = 1 << 2;
}
