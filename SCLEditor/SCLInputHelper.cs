//using System.Collections.Generic;
//using System.IO.Abstractions;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using CSharpFunctionalExtensions;
//using Reductech.EDR.Connectors.FileSystem;
//using Reductech.EDR.Core;
//using Reductech.EDR.Core.Internal;
//using Reductech.EDR.Core.Internal.Errors;
//using Reductech.EDR.Core.Steps;
//using Reductech.EDR.Core.Util;

//namespace Reductech.Utilities.SCLEditor
//{

//public class SCLInputHelper
//{
//    public IEnumerable<SCLInput> GetInputs(IFreezableStep step)
//    {
//        return GetInputs1(step).Distinct();

//        static IEnumerable<SCLInput> GetInputs1(IFreezableStep step)
//        {
//            if (step is CompoundFreezableStep cfs)
//            {
//                //if (step.StepName == nameof(SetVariable<object>))
//                //{
//                //    var vn = cfs.FreezableStepData.TryGetVariableName(
//                //        nameof(SetVariable<object>.Variable),
//                //        typeof(SetVariable<object>)
//                //    );

//                //    if (vn.IsSuccess)
//                //    {
//                //        var valueStep = cfs.FreezableStepData.TryGetStep(
//                //            nameof(SetVariable<object>.Value),
//                //            typeof(SetVariable<object>)
//                //        );

//                //        if (valueStep.IsSuccess
//                //         && valueStep.Value is IConstantFreezableStep)
//                //        {
//                //            yield return new SCLInput.VariableInput(
//                //                vn.Value.Name,
//                //                step.TextLocation
//                //            );
//                //        }
//                //    }
//                //}
//                if (step.StepName == nameof(GetVariable<object>))
//                {
//                    var vn = cfs.FreezableStepData.TryGetVariableName(
//                        nameof(GetVariable<object>.Variable),
//                        typeof(GetVariable<object>)
//                    );

//                    if (vn.IsSuccess)
//                    {
//                        yield return new SCLInput.VariableInput(
//                            vn.Value.Name,
//                            step.TextLocation,
//                            new BoundValue()
//                        );
//                    }
//                }
//                else if (step.StepName == nameof(FileRead))
//                {
//                    var pathStep = cfs
//                        .FreezableStepData
//                        .TryGetStep(nameof(FileRead.Path), typeof(FileRead));

//                    if (pathStep.IsSuccess && pathStep.Value is StringConstantFreezable scf)
//                    {
//                        yield return new SCLInput.FileInput(
//                            scf.Value.GetString(),
//                            step.TextLocation,
//                            new BoundValue()
//                        );
//                    }
//                }

//                foreach (var fsp in cfs.FreezableStepData.StepProperties)
//                {
//                    var childStep = fsp.Value.ConvertToStep();

//                    foreach (var sclInput in GetInputs1(childStep))
//                        yield return sclInput;
//                }
//            }
//        }
//    }
//}

//public abstract record SCLInput(TextLocation TextLocation, BoundValue BoundValue)
//{
//    public record FileInput
//        (string FileName, TextLocation TextLocation, BoundValue BoundValue) : SCLInput(
//            TextLocation,
//            BoundValue
//        )
//    {
//        /// <inheritdoc />
//        public override async Task<Result<Unit, IError>> TrySetup(
//            IStateMonad stateMonad,
//            object data,
//            CancellationToken cancellationToken)
//        {
//            var fileSystem =
//                stateMonad.ExternalContext.TryGetContext<IFileSystem>(
//                    ConnectorInjection.FileSystemKey
//                );

//            if (fileSystem.IsFailure)
//                return fileSystem.MapError(x => x.WithLocation(ErrorLocation.EmptyLocation))
//                    .ConvertFailure<Unit>();

//            await fileSystem.Value.File.WriteAllTextAsync(
//                FileName,
//                data?.ToString(),
//                cancellationToken
//            );

//            return Unit.Default;
//        }
//    }

//    public record VariableInput
//        (string VariableName, TextLocation TextLocation, BoundValue BoundValue) : SCLInput(
//            TextLocation,
//            BoundValue
//        )
//    {
//        /// <inheritdoc />
//        public override async Task<Result<Unit, IError>> TrySetup(
//            IStateMonad stateMonad,
//            object data,
//            CancellationToken cancellationToken)
//        {
//            var r = await
//                stateMonad.SetVariableAsync(
//                    new VariableName(VariableName),
//                    data,
//                    true,
//                    null,
//                    cancellationToken
//                );

//            return r;
//        }
//    }

//    public abstract Task<Result<Unit, IError>> TrySetup(
//        IStateMonad stateMonad,
//        object data,
//        CancellationToken cancellationToken);
//}

//public class BoundValue
//{
//    public string Value { get; set; }
//}

//}


