using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace GunaUI_Patcher
{
    internal class Program
    {
        public const string guna = "Guna.UI2";
        public const string latestTestedVersion = "2.0.4.6";

        [STAThread]
        static void Main(string[] args)
        {

            string folderLoc = Directory.GetCurrentDirectory() + $"\\Cracked\\";
            string fileLoc = $"{folderLoc}{guna}.dll";

            while (!File.Exists(guna + ".dll"))
            {
                Console.WriteLine($"{guna}.dll not found in working directory:\nPlease acquire your own binary and paste it into the same folder as this patcher app.");
                Thread.Sleep(1000);
                Console.Clear();
            }

            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("Reading Guna.UI2.dll, please wait..");
            var assembly = AssemblyDefinition.ReadAssembly("Guna.UI2.dll");

            try
            {
                Console.WriteLine("Accessing GunaUILicenseMgr's class..");
                var type = assembly.MainModule.Types
        .FirstOrDefault(t => t.Namespace == "Guna.UI2.WinForms" && t.Name == "GunaUILicenseMgr");

                if (type == null)
                {
                    Console.WriteLine("license manager class not found!");
                    throw new Exception();
                }

                Console.WriteLine("Accessing the methods called in GunaUILicenseMgr's initializer..");
                var ctor = type.Methods
        .FirstOrDefault(m => m.Name == ".ctor" && m.Parameters.Count == 0);

                if (ctor == null)
                {
                    Console.WriteLine("license manager constructor not found!");
                    throw new Exception();
                }

                Console.WriteLine("\nApplying the patch:");

                var ilProcessor = ctor.Body.GetILProcessor();

                Console.WriteLine($"Filtering the methods that we will modify.. ({ctor.Body.Instructions.Count} found)");
                var methodCalls = ctor.Body.Instructions
                 .Where(instruction => instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                 .Select(instruction => instruction.Operand as MethodReference)
                 .Where(mr => mr != null && mr.Name != "InitializeComponent")
                 .Where(mr => mr.Name != ".ctor" && mr.Name != "Start").ToList();

                if (methodCalls.Count == 0)
                {
                    Console.WriteLine("no methods to replace (patch not applied)");
                    throw new Exception();
                }

                Console.WriteLine(methodCalls.Count + " method(s) left after filtering!");
                foreach (var methodRef in methodCalls)
                {
                    Console.WriteLine($" - {methodRef.Name} (parent: {methodRef.DeclaringType.Resolve()})");
                }

                foreach (var methodRef in methodCalls)
                {
                    var parentClass = methodRef.DeclaringType;

                    var parentClassDef = parentClass.Resolve();
                    if (parentClassDef == null)
                    {
                        Console.WriteLine($"failed to resolve definition: {parentClass.FullName}");
                        continue;
                    }

                    Console.WriteLine($"modifying methods in: {parentClass.Name}");

                    foreach (var method in parentClassDef.Methods)
                    {
                        if (method.HasBody)
                        {
                            Console.WriteLine($" replacing method: {method.Name}");
                            var newBody = new MethodBody(method)
                            {
                                Instructions = { Instruction.Create(OpCodes.Ret) }
                            };

                            method.Body = newBody;
                        }
                    }
                }

                Console.WriteLine($"Patch finished!");

                Console.WriteLine($"\nShould be cracked, cleaning up code..");
                ilProcessor.Body.SimplifyMacros();

                Console.WriteLine("Writing to .dll file..");

                Directory.CreateDirectory(folderLoc);
                assembly.Write(fileLoc);
                Console.WriteLine($"\nSUCCESS! - \"{fileLoc}\" (took {sw.ElapsedMilliseconds}ms)");
                Console.WriteLine("(We also opened the folder in file explorer.)");

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer",
                    Arguments = $"/select,\"{fileLoc}\"",
                    UseShellExecute = true
                });
            }
            catch
            {
                Console.WriteLine("\nPatch failed!");
                Console.WriteLine($"There might've been an update that broke the patcher!\n\nLatest tested version: {latestTestedVersion}");

            }

            Console.ReadKey();
        }
    }
}
