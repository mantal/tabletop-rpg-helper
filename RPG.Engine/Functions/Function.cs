using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using RPG.Engine.Ids;

namespace RPG.Engine.Functions
{
	public abstract record Function
	{
		public static readonly Regex ArgumentRegex =
			new Regex("^\\$[1-9]\\d?$", RegexOptions.Compiled | RegexOptions.Singleline);

		public FunctionId Id { get; }
		public int RequiredParameterNumber { get; protected init; }
		public int? MaxParameterNumber { get; protected init; }
		public int? ParameterBatchSize { get; protected init; }
		public IReadOnlyList<ArgumentType> ParameterTypes { get; protected init; }

		protected Function(FunctionId id)
		{
			Id = id;
		}
		
		public abstract double Execute(object[] args);
	}
}