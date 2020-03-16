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
        private readonly IDictionary<string, double> _cache = new Dictionary<string, double>();

        public double Get(StatId id)
		{
			if (_cache.ContainsKey(id)) return _cache[id];

            var stat = Stats[id];
            
			var result = stat.Base;
			foreach (var modifier in stat.Modifiers)
			{
				result = modifier.Type.Apply(result, modifier.RoundingMethod.Convert(Get(modifier.StatId) * modifier.Multiplier));
            }

			var value = stat.RoundingMethod.Convert(result);

            _cache.Add(id, value);
			return value;
		}

		public bool Add(StatId id, Stat stat)
		{
            _cache.Clear();
			return Stats.TryAdd(id, stat);
		}

		public bool Exists(StatId id) => Stats.ContainsKey(id);
	}
}
