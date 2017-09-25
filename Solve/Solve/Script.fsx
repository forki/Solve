﻿#load "Main.fs"

open Solve
open System.Diagnostics

[<DebuggerStepThrough>]
let check errorName expected actual = if actual = expected then expected else (failwithf "%s. %O != %O" errorName actual expected)

[<DebuggerStepThrough>]
let sn x = AnyTyped(TypedSNumber(SNumber x))
[<DebuggerStepThrough>]
let sv x = AnyVariable(Variable(x))

[<DebuggerStepThrough>]
let snp x = Parameter(sn x)
[<DebuggerStepThrough>]
let vp n = Parameter(AnyVariable(Variable(n)))

[<DebuggerStepThrough>]
let sna x = Argument(sn x)
[<DebuggerStepThrough>]
let va n = Argument(AnyVariable(Variable(n)))

module TestExecutionModule =
    module testUnifyParamsWithArguments =
        let test() = 
            let u1 = ExecutionModule.unifyParamsWithArguments [vp "N"] [va "N2"] |> check "u1" (Some([sv "N"]))
            let u2 = ExecutionModule.unifyParamsWithArguments [snp 1.] [va "N"] |> check "u2" (Some([sn 1.]))
            let u3 = ExecutionModule.unifyParamsWithArguments [vp "N"] [sna 1.] |> check "u3" (Some([sn 1.]))
            let u4 = ExecutionModule.unifyParamsWithArguments [snp 1.] [sna 1.] |> check "u4" (Some([sn 1.]))
            let u5 = ExecutionModule.unifyParamsWithArguments [snp 1.] [sna 2.] |> check "u5" None
            ()

    module testUnifyExpression = 
        let test() = 
            let expr1 = 
                ExecutionModule.unifyExpression (EqExpr(sv "N", sn 1.)) (fun (Variable(v)) -> sn 1.)
                |> check "unif1" (EqExpr(sn 1., sn 1.))
            let expr2 = 
                ExecutionModule.unifyExpression (EqExpr(sv "N", sv "N2")) (fun (Variable(v)) -> if v = "N" then sn 1. else sn 2.)
                |> check "unif2" (EqExpr(sn 1., sn 2.))
            let expr3 = 
                ExecutionModule.unifyExpression (EqExpr(sv "N", sv "N2")) (fun (Variable(v)) -> if v = "N" then sn 1. else sv v)
                |> check "unif3" (EqExpr(sn 1., sv "N2"))
            ()

    module testUnifyRule = 
        let test() = 
            let rule1 = 
                ExecutionModule.unifyRule (Rule(Signature("eq1", [vp "N"]), (EqExpr(sv "N", sn 1.)))) [sna 1.]
                |> check "rule1" (Some(Rule(Signature("eq1", [snp 1.]), (EqExpr(sn 1., sn 1.)))))
            ()

    module testExecuteCalc = 
        let test() =
            let calc1 = 
                ExecutionModule.executeCalc (Value(CalcAny(sn 1.)))
                |> check "calc1" (SNumber(1.))
            let calc2 = 
                ExecutionModule.executeCalc (Plus(CalcAny(sn 1.), CalcAny(sn 1.)))
                |> check "calc2" (SNumber(2.))
            ()

    module testExecuteExpression = 
        let test() = 
            let executeCustom a = failwith "unexpected input"
            let exExp1 = 
                ExecutionModule.executeExpression (EqExpr(sv "N", sn 1.)) executeCustom (fun v -> sn 1.)
                |> check "ex exp1" [EqExpr(sn 1., sn 1.)]
            let exExp2 = 
                ExecutionModule.executeExpression (EqExpr(sv "N", sn 1.)) executeCustom (fun v -> AnyVariable(v))
                |> check "ex exp1" [EqExpr(sn 1., sn 1.)]
            let exExp3 = 
                ExecutionModule.executeExpression (AndExpression(CalcExpr(sv "N", Value(CalcAny(sn 1.))), EqExpr(sv "N", sn 1.))) executeCustom (fun v -> sn 1.)
                |> check "ex exp3" [AndExpression(CalcExpr(sn 1., Value(CalcAny(sn 1.))), EqExpr(sn 1., sn 1.))]
            ()

    module testExecute = 
        let test() = 
            let ex1 = 
                ExecutionModule.checkGoal (Goal("eq1", [va "N"])) [Rule(Signature("eq1", [vp "N"]), (EqExpr(sv "N", sn 1.)))]
                |> check "execute1" [[sn 1.]]
            let ex2 = 
                ExecutionModule.checkGoal (Goal("eq2", [sna 1.])) [Rule(Signature("eq2", [vp "N"]), (EqExpr(sv "N", sn 1.)))]
                |> check "execute2" [[sn 1.]]
            let ex3 = 
                ExecutionModule.checkGoal (Goal("eq3", [sna 2.])) [Rule(Signature("eq3", [vp "N"]), (EqExpr(sv "N", sn 1.)))]
                |> check "execute3" []
            
            let andCheck =
                ExecutionModule.checkGoal (Goal("and", [va "N"])) [Rule(Signature("and", [vp "N"]), (AndExpression(EqExpr(sv "N", sn 1.), EqExpr(sv "N", sn 2.))))]
                |> check "execute3" []

            let orCheck =
                ExecutionModule.checkGoal (Goal("or", [va "N"])) [Rule(Signature("or", [vp "N"]), (OrExpression(EqExpr(sv "N", sn 1.), EqExpr(sv "N", sn 2.))))]
                |> check "execute3" [[sn 1.]; [sn 2.]]

            let falseCheck =
                ExecutionModule.checkGoal (Goal("fa", [va "N"])) [Rule(Signature("fa", [vp "N"]), (False))]
                |> check "false check" []

            let notCheck =
                ExecutionModule.checkGoal (Goal("fa", [va "N"])) [Rule(Signature("fa", [vp "N"]), (NotExpression(True)))]
                |> check "not true check" []

            let innerVariableCheck =
                ExecutionModule.checkGoal (Goal("innervar", [va "N"])) [Rule(Signature("innervar", [vp "N"]), (AndExpression(EqExpr(sv "Temp", sn 1.), EqExpr(sv "N", sv "Temp"))))]
                |> check "innervar not supported" [[sn 1.]]
            ()

    module RealTest =
        let test() =
            let eq1 = 
                ExecutionModule.checkGoal (Goal("eq1_both", [va "N"; va "Res"])) [Rule(Signature("eq1_both", [vp "N1"; vp "N2"]), (AndExpression((EqExpr(sv "N1", sn 1.), (EqExpr(sv "N2", sn 1.))))))]
                |> check "and test1" [[sn 1.; sn 1.]]
            let eq2 = 
                ExecutionModule.checkGoal(Goal("eq", [va "N"; va "N2"])) [Rule(Signature("eq", [vp "N1"; vp "N2"]), (EqExpr(sv "N1", sv "N2")))]
                |> check "eq test" [[sv "N2"; sv "N2"]]

            let f = Rule(Signature("f1", [vp "N"; vp "Res"]), OrExpression(AndExpression(EqExpr(sv "N", sn 1.), EqExpr(sv "Res", sn 1.)), AndExpression(GrExpr(sv "N", sn 1.), EqExpr(sv "Res", sn 2.))))
            let f1 = 
                ExecutionModule.checkGoal (Goal("f1", [sna 1.; va "Res"])) [f]
                |> check "f1" [[sn 1.; sn 1.]]
            let f2 =
                ExecutionModule.checkGoal (Goal("f1", [sna 3.; va "Res"])) [f]
                |> check "f2" [[sn 3.; sn 2.]]

            let factorial : Rule = (("factorial" /=> [variable "N"; variable "Res"])) ==> (fun [Parameter(N); Parameter(Res)] -> ((N /> AnyTyped(snum1)) /& (CalcExpr (N, inc (CalcAny N)))) /| (Res /= (sn 1.)))
            let f3 = 
                ExecutionModule.checkGoal (Goal("factorial", [sna 3.; va "Res"])) [factorial]
                |> check "f3" [[sn 3.; sn 1.]]
            ()

let executeAll =
    TestExecutionModule.testUnifyParamsWithArguments.test()
    TestExecutionModule.testUnifyExpression.test()
    TestExecutionModule.testUnifyRule.test()
    TestExecutionModule.testExecuteCalc.test()
    TestExecutionModule.testExecuteExpression.test()
    TestExecutionModule.testExecute.test()
    TestExecutionModule.RealTest.test()