using System.Collections.Generic;

namespace RPG.Services
{
	public class StatService
    {
        public IDictionary<string, Stat> Stats = new Dictionary<string, Stat>
        {
            {
                "CON",
                new Stat
                {
                    Base = 10,
                }
            },
			{
                "FOR",
                new Stat
				{
                    Base = 10,
				}
			},
            {
                "HP",
                new Stat
                {
                    Base = 10,
                    Modifiers = new [] { new Modifier("CON", ModifierType.Add), },
                }
            },
			{
				"ATT",
				new Stat
				{
					Base = 100,
					Modifiers = new [] { new Modifier("FOR", ModifierType.Add, 2), },
				}
			},
        };

        public int Get(StatId id)
        {
            var stat = Stats[id];
            
			double result = stat.Base;
			foreach (var modifier in stat.Modifiers)
			{
				result = modifier.Type.Apply(result, modifier.IntConversionMethod.Convert(Get(modifier.StatId) * modifier.Multiplier));
            }

			return (int) stat.IntConversionMethod.Convert(result);
		}

		public bool Add(StatId id, Stat stat) => Stats.TryAdd(id, stat);

		public bool Exists(StatId id) => Stats.ContainsKey(id);
	}
}
