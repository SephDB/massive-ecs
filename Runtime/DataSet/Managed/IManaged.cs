namespace Massive
{
	public interface IManaged<T> where T : IManaged<T>
	{
		void CopyTo(ref T other);

		private static void ReflectionSupportForAOT()
		{
			// ReSharper disable ObjectCreationAsStatement
			new MassiveManagedDataSet<T>();
		}
	}
}
