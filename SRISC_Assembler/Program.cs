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
        delegate V MyDelegate<T, U, V>(T input, out U output);
        static Dictionary<string, MyDelegate<string, List<string>, bool>> Lexicon = new Dictionary<string, MyDelegate<string, List<string>, bool>>()
        {
            { "byte", VariableManager.Handler }
        };

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
            { "br", Branch }
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
            { "<" ,"slt" },
            { "==", "equ" }
        };

        static List<string> Insns = new List<string>();
        static Dictionary<string, int> Labels = new Dictionary<string, int>();

        static void Main()
        {
            List<string> nah;
            VariableManager.Handler("byte a = 1", out nah);
            VariableManager.Handler("a++", out nah);
            VariableManager.Handler("a--", out nah);
            VariableManager.Handler("byte b = 2", out nah);
            VariableManager.Handler("a = ~b", out nah);
            VariableManager.Handler("b < a", out nah);
            VariableManager.Handler("b == 2", out nah);
            VariableManager.Handler("b < 2", out nah);
            VariableManager.Handler("byte c = 3", out nah);
            VariableManager.Handler("byte d = 4", out nah);
            VariableManager.Handler("byte e = 5", out nah);
            VariableManager.Handler("byte b = a", out nah);
            VariableManager.Handler("b = a + 5", out nah);
            VariableManager.Handler("c = 2 + 5", out nah);
            VariableManager.Handler("c = a + d", out nah);

            string path = "testprogram.txt";
            string[] code = File.ReadAllLines(path);
            int labelCnt = 0;

            // Label and mnemonic conversion
            for (int i = 0; i < code.Length; i++)
            {
                string line = code[i];

                // Check code for labels
                if (line.Contains(':'))
                {
                    Labels.Add(line.Split(':')[0], i - labelCnt);
                    labelCnt++;
                }
                else
                {
                    // Convert basic insn to binary
                    Insns.Add(Mnemonics[line.Split(' ')[0]](line));

                    // Increment age of registers
                    //for (int r = 112; r <= 115; r++)
                    //{
                    //    if (Variables.ElementAt(r).Value.Active)
                    //    {
                    //        Variables.ElementAt(r).Value.LastModified++;
                    //    }
                    //}
                }
            }

            Insns.ForEach(o => Debug.WriteLine(Insns.IndexOf(o).ToString() + ": " + o));
            Labels.ToList().ForEach(o => Debug.WriteLine(o.Key + ": " + o.Value));
        }

        public static string ConvertLiteral(string radix, string value)
        {
            return radix switch
            {
                "h" => Convert.ToString(Convert.ToInt32(value, 16), 2).PadLeft(8, '0'), // hex
                "b" => Convert.ToString(Convert.ToInt32(value, 2), 2).PadLeft(8, '0'),  // bin
                _ => Convert.ToString(Convert.ToInt32(value), 2).PadLeft(8, '0'),       // dec
            };
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
                case "inc":     target = false; opsel = "1000"; break;
                case "dec":     target = false; opsel = "1001"; break;
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
            bool io;

            string mnemonic = line.Split(' ')[0];
            switch (mnemonic)
            {
                case "load":    write = false; io = false; break;
                case "store":   write = true; io = false; break;
                case "read":    write = false; io = true; break;
                case "write":   write = true; io = true; break;
                default:        write = false; io = false; break;
            }

            string insn = "10";
            string r = line.Substring(line.IndexOf(' ')).Split('r')[1].Substring(0, 1);
            insn += DecodeR(r);

            if (write) insn += "1";
            else insn += "0";

            if (line.Contains(','))
            {
                if (io)
                {
                    insn += Convert.ToString(Convert.ToByte(line.Split(',')[1].Substring(1)), 2).PadLeft(4, '0').PadLeft(7, '1');
                }
                else
                {
                    insn += Convert.ToString(Convert.ToByte(line.Split(',')[1].Substring(1)) + 1, 2).PadLeft(7, '0');
                }
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

        static string DecodeR(string r)
        {
            switch (r)
            {
                case "1": return "01";
                case "2": return "10";
                case "3": return "11";
                default: return "00";
            }
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
