﻿using HeuristicLab.Common;
using HeuristicLab.Core;
using HeuristicLab.Data;
using HeuristicLab.Operators;
using HeuristicLab.Optimization.Operators;
using HeuristicLab.Parameters;
using HeuristicLab.Persistence.Default.CompositeSerializers.Storable;
using HeuristicLab.Problems.CFG;

namespace HeuristicLab.Algorithms.CFG.MultiObjective
{
    [Item("CFGSolutionsCreator", "An operator which creates new solutions and initialize solution with given program. Evaluation of the new solutions is executed in parallel, if an engine is used which supports parallelization.")]
    [StorableClass]
    internal class CFGSolutionCreator: SingleSuccessorOperator
    {
        public ValueLookupParameter<IntValue> NumberOfSolutionsParameter
        {
            get { return (ValueLookupParameter<IntValue>)Parameters["NumberOfSolutions"]; }
        }
        public ValueLookupParameter<IOperator> SolutionCreatorParameter
        {
            get { return (ValueLookupParameter<IOperator>)Parameters["SolutionCreator"]; }
        }
        public ValueLookupParameter<IOperator> EvaluatorParameter
        {
            get { return (ValueLookupParameter<IOperator>)Parameters["Evaluator"]; }
        }
        public ValueLookupParameter<BoolValue> ParallelParameter
        {
            get { return (ValueLookupParameter<BoolValue>)Parameters["Parallel"]; }
        }
        private ScopeParameter CurrentScopeParameter
        {
            get { return (ScopeParameter)Parameters["CurrentScope"]; }
        }
        
        public IScope CurrentScope
        {
            get { return CurrentScopeParameter.ActualValue; }
        }
        public IntValue NumberOfSolutions
        {
            get { return NumberOfSolutionsParameter.Value; }
            set { NumberOfSolutionsParameter.Value = value; }
        }

        public ValueParameter<InitialGivenProgramCreator> InitialGivenProgramCreator
        {
            get { return (ValueParameter<InitialGivenProgramCreator>)Parameters["initialGivenProgramCreator"]; }
        }

        [StorableConstructor]
        private CFGSolutionCreator(bool deserializing) : base(deserializing) { }
        private CFGSolutionCreator(CFGSolutionCreator original, Cloner cloner) : base(original, cloner) { }
        public CFGSolutionCreator()
          : base()
        {
            Parameters.Add(new ValueLookupParameter<IntValue>("NumberOfSolutions", "The number of solutions that should be created."));
            Parameters.Add(new ValueLookupParameter<IOperator>("SolutionCreator", "The operator which is used to create new solutions."));
            Parameters.Add(new ValueLookupParameter<IOperator>("Evaluator", "The operator which is used to evaluate new solutions. This operator is executed in parallel, if an engine is used which supports parallelization."));
            Parameters.Add(new ValueLookupParameter<BoolValue>("Parallel", "True if the operator should be applied in parallel on all sub-scopes, otherwise false.", new BoolValue(true)));
            Parameters.Add(new ScopeParameter("CurrentScope", "The current scope to which the new solutions are added as sub-scopes."));
            Parameters.Add(new ValueParameter<InitialGivenProgramCreator>("initialGivenProgramCreator", "The operator to add program to initial population", new InitialGivenProgramCreator()));
            

        }
        [StorableHook(HookType.AfterDeserialization)]
        private void AfterDeserialization()
        {
            if (!Parameters.ContainsKey("Parallel")) Parameters.Add(new ValueLookupParameter<BoolValue>("Parallel", "True if the operator should be applied in parallel on all sub-scopes, otherwise false.", new BoolValue(true))); // backwards compatibility
        }

        public override IDeepCloneable Clone(Cloner cloner)
        {
            return new CFGSolutionCreator(this, cloner);
        }

        public override IOperation Apply()
        {
            int count = NumberOfSolutionsParameter.ActualValue.Value;
            IOperator creator = SolutionCreatorParameter.ActualValue;
            IOperator initialProgramCreator = (IOperator)InitialGivenProgramCreator.ActualValue;
            IOperator evaluator = EvaluatorParameter.ActualValue;
            bool parallel = ParallelParameter.ActualValue.Value;

            int current = CurrentScope.SubScopes.Count;
            for (int i = 0; i < count; i++)
                CurrentScope.SubScopes.Add(new Scope((current + i).ToString()));

            OperationCollection creation = new OperationCollection();
            OperationCollection evaluation = new OperationCollection() { Parallel = parallel };


            for (int i = 0; i < count; i++)
            {
                if (i == 0)
                {
                    if (initialProgramCreator != null) creation.Add(ExecutionContext.CreateOperation(initialProgramCreator, CurrentScope.SubScopes[current + i]));
                    if (evaluator != null) evaluation.Add(ExecutionContext.CreateOperation(evaluator, CurrentScope.SubScopes[current + i]));
                }
                else
                {
                    if (creator != null) creation.Add(ExecutionContext.CreateOperation(creator, CurrentScope.SubScopes[current + i]));
                    if (evaluator != null) evaluation.Add(ExecutionContext.CreateOperation(evaluator, CurrentScope.SubScopes[current + i]));
                }
                
            }
            OperationCollection next = new OperationCollection();
            next.Add(creation);
            next.Add(evaluation);
            next.Add(base.Apply());
            return next;
        }
    }
}
