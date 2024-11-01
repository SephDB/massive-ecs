﻿namespace Massive
{
	public class MassiveRegistryConfig : RegistryConfig
	{
		public int FramesCapacity = Constants.DefaultFramesCapacity;

		public MassiveRegistryConfig() { PageSize = 1024; }
	}
}
