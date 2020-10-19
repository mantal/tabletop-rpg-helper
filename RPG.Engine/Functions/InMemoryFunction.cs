using System;
using System.Collections.Generic;
using System.Linq;
using RPG.Engine.Ids;

namespace RPG.Engine.Functions
{
	public class InMemoryFunction : Function
	{
		private readonly Func<object[], double> _apply;

		
		public InMemoryFunction(FunctionId id,
								int requiredParameterNumber,
								Func<object[], double> apply)
			: this(id, requiredParameterNumber, null, null, null, apply)
		{ }

		public InMemoryFunction(FunctionId id,
								int requiredParameterNumber,
								int? maxParameterNumber,
								Func<object[], double> apply)
			: this(id, requiredParameterNumber, maxParameterNumber, null, null, apply)
		{ }

		public InMemoryFunction(FunctionId id,
								int requiredParameterNumber,
								int? maxParameterNumber,
								int? parameterBatchSize,
								IEnumerable<Type>? parametersTypes,
								Func<object[], double> apply)
			: base(id)
		{
			if (parameterBatchSize > maxParameterNumber)
				throw new ArgumentOutOfRangeException(nameof(parameterBatchSize), $"{nameof(parameterBatchSize)} cannot be greater than {nameof(maxParameterNumber)}");
			
			RequiredParameterNumber = requiredParameterNumber;
			MaxParameterNumber = maxParameterNumber;
			ParameterBatchSize = parameterBatchSize;
			ParameterTypes = parametersTypes?.ToList().AsReadOnly() ?? Enumerable
																	   .Repeat(typeof(double),
																			   MaxParameterNumber
																			   ?? ParameterBatchSize
																			   ?? RequiredParameterNumber)
																	   .ToList()
																	   .AsReadOnly();
			_apply = apply;
		}

		public override double Execute(object[] parameters)
		{
			if (parameters.Length < RequiredParameterNumber)
				throw new ArgumentException($"not enough argument to call function {Id}, required {RequiredParameterNumber} but got {parameters.Length}", nameof(parameters));

			return _apply(parameters);
		}
	}
}