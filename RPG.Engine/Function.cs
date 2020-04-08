using System;
using RPG.Engine.Ids;

namespace RPG.Engine.Services
{
	public class Function
	{
		public FunctionId Id { get; }
		public int RequiredParameterNumber { get; }
		public int? MaxParameterNumber { get; }
		public int? ParameterBatchSize { get; }
		private readonly Func<double[], double> _apply;
		
		public Function(FunctionId id,
						int requiredParameterNumber,
						Func<double[], double> apply)
			: this(id, requiredParameterNumber, null, null, apply)
		{ }

		public Function(FunctionId id,
						int requiredParameterNumber,
						int? maxParameterNumber,
						Func<double[], double> apply)
			: this(id, requiredParameterNumber, maxParameterNumber, null, apply)
		{ }

		public Function(FunctionId id,
						int requiredParameterNumber,
						int? maxParameterNumber,
						int? parameterBatchSize,
						Func<double[], double> apply)
		{
			if (maxParameterNumber != null && parameterBatchSize != null && parameterBatchSize > maxParameterNumber)
				throw new ArgumentOutOfRangeException(nameof(parameterBatchSize), $"{nameof(parameterBatchSize)} cannot be greater than {nameof(maxParameterNumber)}");
			Id = id ?? throw new ArgumentNullException(nameof(id));
			RequiredParameterNumber = requiredParameterNumber;
			ParameterBatchSize = parameterBatchSize;
			_apply = apply;
			MaxParameterNumber = maxParameterNumber;
		}

		public double Execute(params double[] parameters)
		{
			if (parameters.Length < RequiredParameterNumber)
				throw new ArgumentException(nameof(parameters));

			if (_apply != null)
				return _apply(parameters);
			return 0;
		}
	}
}