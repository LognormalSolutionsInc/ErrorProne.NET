﻿using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using RoslynNunitTestRunner;

namespace ErrorProne.NET.StructAnalyzers.Tests
{
    [TestFixture]
    public class MakeStructReadOnlyAnalyzerTests : CSharpAnalyzerTestFixture<MakeStructReadOnlyAnalyzer>
    {
        public const string DiagnosticId = MakeStructReadOnlyAnalyzer.DiagnosticId;

        [Test]
        public void ThisAssignmentShouldPreventTheWarning()
        {
            string code = @"struct SelfAssign {
    public readonly int Field;

    public void M(SelfAssign other) {
if (other.Field > 0)      
this = other;
    }
}";
            NoDiagnostic(code, DiagnosticId);

            string code2 = @"struct SelfAssign {
    public readonly int Field;

    public void M(SelfAssign other) => 
      this = other;
    
}";

            NoDiagnostic(code2, DiagnosticId);

            string code3 = @"struct SelfAssign
        {
            public readonly int Field;
            public SelfAssign(int f) => Field = f;

            public void Foo()
            {
                ref SelfAssign x = ref this;
                x = new SelfAssign(42);
            }
        }";
            NoDiagnostic(code3, DiagnosticId);
        }

        [Test]
        public void ThisAssignmentInPropertyShouldPreventTheWarning()
        {
            string code = @"struct SelfAssign2
        {
            public int X
            {
                get
                {
                    this = new SelfAssign2();
                    return 32;
                }
            }
        }";

            NoDiagnostic(code, DiagnosticId);

            string code2 = @"struct SelfAssign2
        {
            public int X
            {
                set
                {
                    this = new SelfAssign2();
                }
            }
        }";

            NoDiagnostic(code, DiagnosticId);
        }

        [Test]
        public void HasDiagnosticSelfAssignmentInConstructor()
        {
            string code = @"struct [|SelfAssign2|]
        {
            private readonly int _x;
            public SelfAssign2(int x)
            {
                _x = x;
                this = new SelfAssign2();
            }
        }";
            HasDiagnostic(code, DiagnosticId);
        }

        [Test]
        public void HasDiagnosticForRefReadonly()
        {
            string code = @"struct [|SelfAssign|]
        {
            public readonly int Field;
            public SelfAssign(int f) => Field = f;

            public void Foo()
            {
                ref readonly SelfAssign x = ref this;
            }
        }";
            HasDiagnostic(code, DiagnosticId);
        }

        [Test]
        public void HasDiagnosticForIn()
        {
            string code = @"struct [|SelfAssign|]
        {
            public readonly int Field;
            public SelfAssign(int f) => Field = f;

            public void Foo()
            {
                Bar(in this);
            }
            public void Bar(in SelfAssign sa) {}
        }";

            HasDiagnostic(code, DiagnosticId);
        }

        [Test]
        public void HasDiagnosticsForEmptyStruct()
        {
            string code = @"struct [|FooBar|] {}";
            HasDiagnostic(code, DiagnosticId);
        }

        [TestCaseSource(nameof(GetHasDiagnosticCases))]
        public void HasDiagnosticCases(string code)
        {
            HasDiagnostic(code, DiagnosticId);
        }

        public static IEnumerable<string> GetHasDiagnosticCases()
        {
            // With constructor only
            yield return 
@"struct [|FooBar|] {
    public FooBar(int n):this() {}
}";

            // With one private field
            yield return
@"struct [|FooBar|] {
    private readonly int _x;
    public FooBar(int x) => _x = x;
}";
            
            // With one public readonly property
            yield return
@"struct [|FooBar|] {
    public int X {get;}
    public FooBar(int x) => X = x;
}";
            
            // With one public get-only property
            yield return
@"struct [|FooBar|] {
    public int X => 42;
}";
            
            // With one public readonly property and method
            yield return
@"struct [|FooBar|] {
    public int X {get;}
    public FooBar(int x) => X = x;
    public int GetX() => X;
}";
            
            // With const
            yield return
@"struct [|FooBar|] {
    private readonly int _x;
    private const int MaxLength = 1;
}";
            // With indexer
            yield return
@"struct [|FooBar|]<T> {
    private readonly T[] _buffer;
    public T Value
        {
            get
            {
                return _buffer[Index];
            }

            set
            {
                _buffer[Index] = value;
            }
        }
}";
            
            // With getter and setter
            yield return
@"struct [|FooBar|] {
    public int Value
        {
            get
            {
                return 42;
            }

            set
            {
                
            }
        }
}";
            
            // With getter and setter as expression-bodies
            yield return
@"struct [|FooBar|] {
  private readonly object[] _data;  
  public int Value
        {
            get => 42;

            set => _data[0] = value;
        }
}";

        }

        [Test]
        public void NoDiagnosticCasesWhenStructIsAlreadyReadonly()
        {
            string code = @"readonly struct FooBar {}";
            NoDiagnostic(code, DiagnosticId);
        }

        [Test]
        public void NoDiagnosticCasesWhenStructIsAlreadyReadonlyWithPartialDeclaation()
        {
            string code = @"partial struct FooBar {} readonly partial struct FooBar {}";
            NoDiagnostic(code, DiagnosticId);
        }

        [TestCaseSource(nameof(GetNoDiagnosticCases))]
        public void NoDiagnosticCases(string code)
        {
            NoDiagnostic(code, DiagnosticId);
        }

        public static IEnumerable<string> GetNoDiagnosticCases()
        {
            // Enums should not be readonly
            yield return @"enum FooBar {}";

            // Already marked with 
            yield return 
@"readonly struct FooBar {
    public FooBar(int n):this() {}
}";

            // Non-readonly field
            yield return
@"struct FooBar {
    private int _x;
}";
            
            // With a setter
            yield return
@"struct FooBar {
    public int X {get; private set;}
}";

        }
    }
}