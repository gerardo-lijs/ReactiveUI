﻿// Copyright (c) 2020 .NET Foundation and Contributors. All rights reserved.
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Mono.Cecil.Cil;

namespace ReactiveUI.Fody.InstructionPatternMatching
{
    internal class OptionalPatternInstruction : PatternInstruction
    {
        public OptionalPatternInstruction(OpCode[] eligibleOpCodes, Terminal? terminal = null, InstructionPredicate? predicate = null)
            : base(eligibleOpCodes, terminal, predicate)
        {
        }

        public OptionalPatternInstruction(OpCode opCode, Terminal? terminal = null, InstructionPredicate? predicate = null)
            : base(opCode, terminal, predicate)
        {
        }
    }
}
