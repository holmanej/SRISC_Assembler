using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SRISC_Assembler
{
    static class Program
    {
        static Dictionary<string, Func<string, string>> Mnemonics = new Dictionary<string, Func<string, string>>()
        {
            { "imm", Imm },
            { "add", ALU },
            { "sub", ALU },
            { "and", ALU },
            { "or", ALU },
            { "xor", ALU },
            { "inv", ALU },
            { "sll", ALU },
            { "srl", ALU },
            { "inc", ALU },
            { "dec", ALU },
            { "ind", ALU },
            { "ult", ALU },
            { "slt", ALU },
            { "equ", ALU },
            { "eqz", ALU },
            { "set", ALU },
            { "load", Mem },
            { "store", Mem },
            { "read", Mem },
            { "write", Mem },
            { "br", Branch },
            { "exec", Exec },
            { "nop", Nop }
        };

        public static Dictionary<string, string> AssignOps = new Dictionary<string, string>()
        {
            { "+", "add" }, 
            { "-", "sub" },
            { "&", "and" }, 
            { "|", "or" }, 
            { "^", "xor" },
            { "<<", "sll" },
            { ">>", "srl" }
        };

        public static Dictionary<string, string> CmpOps = new Dictionary<string, string>()
        {            
            { "u<", "ult" },
            { "<" , "slt" },
            { "==", "equ" }
        };

        static List<string> Insns = new List<string>();
        static Dictionary<string, int> Labels = new Dictionary<string, int>();

        static int Main(string[] args)
        {
            {
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                //List<string> nah;
                //VariableManager.Handler("byte a = 1", out nah);
                //VariableManager.Handler("a++", out nah);
                //VariableManager.Handler("a--", out nah);
                //VariableManager.Handler("byte b = 2", out nah);
                //VariableManager.Handler("a = ~b", out nah);
                //VariableManager.Handler("b < a", out nah);
                //VariableManager.Handler("b == 2", out nah);
                //VariableManager.Handler("b < 2", out nah);
                //VariableManager.Handler("byte c = 3", out nah);
                //VariableManager.Handler("byte d = 4", out nah);
                //VariableManager.Handler("byte e = 5", out nah);
                //VariableManager.Handler("byte b = a", out nah);
                //VariableManager.Handler("b = a + 5", out nah);
                //VariableManager.Handler("b = [a] + 5", out nah);
                //VariableManager.Handler("c = 2 + 5", out nah);
                //VariableManager.Handler("c = a + d", out nah);
                //sw.Stop();
                //Debug.WriteLine(sw.Elapsed);
            }

            bool success = true;
            string path;
            if (args.Length == 0)
            {
                Console.WriteLine("File not found");
                path = Console.ReadLine();
            }
            else
            {
                path = args[0];
            }
            
            string[] code = File.ReadAllLines(path);
            int labelCnt = 0;
            List<string> asm = new List<string>();

            // Clean out comments and white space
            for (int i = 0; i < code.Length; i++)
            {
                string line = code[i].Split('#')[0].Trim();
                if (line.Length > 0)
                {
                    asm.Add(line);
                }
            }

            // Grab header
            List<string> header = new List<string>();
            int header_beg = asm.FindIndex(s => s == "{") + 1;
            int header_len = asm.FindIndex(s => s == "}") - header_beg;
            header.AddRange(asm.GetRange(header_beg, header_len));
            //header.ForEach(s => Console.WriteLine(s));
            asm.RemoveRange(0, header_len + 2);

            // Label gathering
            for (int i = 0; i < asm.Count; i++)
            {
                string line = asm[i];

                // Check code for labels
                if (line.Contains(':'))
                {
                    Labels.Add(line.Split(':')[0].Trim(), i);
                    labelCnt++;
                    asm.RemoveAt(i);
                    i = 0;
                }
            }            

            for (int i = 0; i < asm.Count; i++)
            {
                string line = asm[i];
                
                // Convert basic insn to binary
                try
                {
                    if (Mnemonics.ContainsKey(line.Split(' ')[0]))
                    {
                        Insns.Add(Mnemonics[line.Split(' ')[0]](line));
                    }
                    else
                    {
                        Console.WriteLine(i + " " + line.Split(' ')[0] + " - Incorrect syntax");
                        success = false;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(i + " " + line + " - " + e.Message);
                    success = false;
                }
            }

            List<string> program_data = new List<string>();            

            for (int i = 0; i < header.Count; i++)
            {
                try
                {
                    program_data.Add(ConvertLiteral(header[i].Substring(0, 1), header[i].Substring(1)));
                }
                catch (Exception e)
                {
                    Console.WriteLine(i + " " + header[i] + " - " + e.Message);
                }
            }

            Insns.ForEach(o => Debug.WriteLine(Insns.IndexOf(o).ToString() + ": " + o));
            Labels.ToList().ForEach(o => Debug.WriteLine(o.Key + ": " + o.Value));

            path = path.Split('.')[0] + ".bin";

            if (args.Length > 1)
            {
                path = args[1] + path;
            }

            List<string> program_header = new List<string>();

            if (args.Length > 2)
            {
                if (args[2] == "guest")
                {
                    List<string> bin = new List<string>();
                    foreach (string s in Insns)
                    {
                        bin.Add(s.Substring(4, 8));
                        bin.Add("0000" + s.Substring(0, 4));
                    }

                    program_header.Add(Convert.ToString(bin.Count % 256, 2).PadLeft(8, '0'));
                    program_header.Add(Convert.ToString(bin.Count / 256, 2).PadLeft(8, '0'));
                    program_header.Add(Convert.ToString(program_data.Count % 256, 2).PadLeft(8, '0'));
                    program_header.Add(Convert.ToString(program_data.Count / 256, 2).PadLeft(8, '0'));
                    File.WriteAllLines(path, program_header);
                    File.AppendAllLines(path, bin);
                    File.AppendAllLines(path, program_data);
                }
            }
            else
            {
                program_header.Add(Convert.ToString(Insns.Count % 256, 2).PadLeft(12, '0'));
                program_header.Add(Convert.ToString(Insns.Count / 256, 2).PadLeft(12, '0'));
                program_header.Add(Convert.ToString(program_data.Count % 256, 2).PadLeft(12, '0'));
                program_header.Add(Convert.ToString(program_data.Count / 256, 2).PadLeft(12, '0'));
                File.WriteAllLines(path, program_header);                
                File.AppendAllLines(path, Insns.ToArray());
                for (int i = 0; i < program_data.Count; i++) program_data[i] = program_data[i].PadLeft(12, '0');
                File.AppendAllLines(path, program_data);
            }

            Console.WriteLine("Program Length: " + Insns.Count);
            program_header.ForEach(s => Console.Write(s + ", "));
            Console.WriteLine("\r\nData size: " + program_data.Count);
            program_data.ForEach(d => Console.Write(d + ", "));
            Console.WriteLine("\r\nLabels:");
            Labels.ToList().ForEach(k => Console.WriteLine(k.Key + "  " + k.Value));
            if (success) Console.WriteLine("Success!");
            Console.WriteLine(path);
            return 0;
        }

        public static string ConvertLiteral(string radix, string value)
        {
            int r = radix switch
            {
                "h" => 16, // hex
                "b" => 2,  // bin
                _ => 10,   // dec
            };

            return Convert.ToString(Convert.ToByte(value, r), 2).PadLeft(8, '0');
        }

        static string Imm(string line)
        {
            string insn = "00";

            string r = line.Split('r')[1].Substring(0, 1);
            insn += DecodeR(r);

            string radix = line.Split(',')[1].Substring(1, 1);
            string value = line.Split(',')[1].Substring(2);
            insn += ConvertLiteral(radix, value);
            return insn;
        }

        static string ALU(string line)
        {
            bool target;
            string opsel;

            string mnemonic = line.Split(' ')[0];
            switch (mnemonic)
            {
                case "add":     target = true; opsel = "0000"; break;
                case "sub":     target = true; opsel = "0001"; break;
                case "and":     target = true; opsel = "0010"; break;
                case "or":      target = true; opsel = "0011"; break;
                case "xor":     target = true; opsel = "0100"; break;
                case "inv":     target = true; opsel = "0101"; break;
                case "sll":     target = true; opsel = "0110"; break;
                case "srl":     target = true; opsel = "0111"; break;
                case "inc":     target = true; opsel = "1000"; break;
                case "dec":     target = true; opsel = "1001"; break;
                case "ind":     target = false; opsel = "1010"; break;
                case "ult":     target = false; opsel = "1011"; break;
                case "slt":     target = false; opsel = "1100"; break;
                case "equ":     target = false; opsel = "1101"; break;
                case "eqz":     target = false; opsel = "1110"; break;
                default:        return "010000001111";
            }

            string insn = "01";

            if (!target) insn += "00";

            string[] parts = line.Substring(line.IndexOf(' ')).Split('r');

            for (int i = 1; i < parts.Length; i++)
            {
                string r = parts[i].Substring(0, 1);
                insn += DecodeR(r);
            }
            
            if (insn.Length < 8) insn += "00";

            return insn + opsel;
        }

        static string Mem(string line)
        {
            bool write;

            string mnemonic = line.Split(' ')[0];
            switch (mnemonic)
            {
                case "load":    write = false; break;
                case "store":   write = true; break;
                case "read":    write = false; break;
                case "write":   write = true; break;
                default:        write = false; break;
            }

            string insn = "10";
            string r = line.Substring(line.IndexOf(' ')).Split('r')[1].Substring(0, 1);
            insn += DecodeR(r);

            if (write) insn += "1";
            else insn += "0";

            if (line.Contains(','))
            {
                string arg = line.Split(',')[1].Substring(1);
                string id = arg.Substring(0, 1);
                string num = arg.Substring(1);
                int addr = id switch
                {
                    "K" => 1,
                    "A" => 16,
                    "T" => 32,
                    "S" => 48,
                    "G" => 80,
                    "D" => 112,
                    _ => throw new Exception("Incorrect Memory ID"),
                };
                if (int.TryParse(num, out int n))
                {
                    addr += n - 1;
                }
                else
                {
                    throw new Exception("Invalid Memory Number");
                }
                //Console.WriteLine(line + "  " + addr);
                insn += Convert.ToString(addr, 2).PadLeft(7, '0');
            }
            else
            {
                insn = insn.PadRight(12, '0');
            }

            return insn;
        }

        static string Branch(string line)
        {
            string insn = "11";
            string field = line.Split(' ')[1];
            if (Labels.ContainsKey(field))
            {
                insn += Convert.ToString(Labels[field], 2).PadLeft(10, '0');
            }
            else
            {
                insn += Convert.ToString(Convert.ToUInt16(field), 2).PadLeft(10, '0');
            }

            return insn;
        }

        static string Exec(string line) => "111111111111";
        static string Nop(string line) => "010000000010";

        static string DecodeR(string r)
        {
            return r switch
            {
                "1" => "01",
                "2" => "10",
                "3" => "11",
                _ => "00",
            };
        }
    }

    public static class Extensions
    {
        public static bool TryEat(this string s, string term, out string newLine)
        {
            if (s.StartsWith(term))
            {
                newLine = s.Trim().Remove(0, term.Length).Trim();
                return true;
            }
            else
            {
                newLine = s;
                return false;
            }
        }
    }
}
