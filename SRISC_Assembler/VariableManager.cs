using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SRISC_Assembler
{
    public class MemObj
    {
        public string Name { get; set; }
        public bool Active { get; set; }
        public int Location { get; set; }
        public int Length { get; set; }
        public int LastModified { get; set; }

        public MemObj(string name, int type, int loc)
        {
            Name = name;
            Active = true;
            Location = loc;
            Length = type;
            LastModified = 0;
        }
    }

    public static class ExtendV
    {
        public static MemObj Var(this List<MemObj> vars, string name)
        {
            return vars.Find(v => v.Name == name);
        }

        public static MemObj VarAt(this List<MemObj> vars, int loc)
        {
            return vars.Find(v => v.Location == loc);
        }

        public static bool Exists(this List<MemObj> vars, string name)
        {
            return vars.Any(v => v.Name == name);
        }

        public static bool Status(this List<MemObj> vars, string name)
        {
            return vars.Find(v => v.Name == name).Active;
        }

        private static bool Status(this List<MemObj> vars, int loc)
        {
            return vars.Find(v => v.Location == loc).Active;
        }

        public static int Location(this List<MemObj> vars, string name)
        {
            return vars.Find(v => v.Name == name).Location;
        }

        public static int FindReg(this List<MemObj> vars)
        {
            for (int i = 112; i <= 115; i++)
            {
                MemObj reg = vars.VarAt(i) ?? new MemObj("", 1, 0) { Active = false };
                if (reg.Active == false)
                {
                    return i;
                }
            }

            return 0;
        }

        public static bool FindMem(this List<MemObj> vars, out int loc)
        {
            for (int i = 1; i <= 111; i++)
            {
                MemObj mem = vars.VarAt(i) ?? new MemObj("", 1, 0); ;
                if (mem.Active == false || mem.Location == 0)
                {
                    loc = i;
                    return true;
                }
            }
            loc = 0;
            return false;
        }

        public static bool CacheVariable(this List<MemObj> vars, string name, bool target)
        {
            List<MemObj> registers = vars.FindAll(v => v.Location >= 112);
            List<MemObj> memory = vars.FindAll(v => v.Location <= 111);
            int regLoc = vars.Location(name);

            if (vars.Exists(name) && (vars.Status(name) || target))
            {
                vars.Var(name).LastModified = 0;
                if (regLoc >= 112)
                {
                    return true;    // var is here already
                }
                else
                {
                    vars.ReserveRegister(out regLoc);   // var will be here
                    if (vars.Status(name))
                    {
                        int varLoc = memory.Location(name); // var is stored here
                        VariableManager.Code.Add("load r" + (regLoc - 112).ToString() + ", " + varLoc.ToString());  // "load regLoc varLoc"
                    }
                    vars.Var(name).Location = regLoc;   // move var to registers
                    vars.Var(name).LastModified = 0;   // reset modified
                    if (target) vars.Var(name).Active = true;

                    return true;
                }
            }
            else
            {
                if (vars.Exists(name)) Debug.WriteLine(name + " unassigned");
                else Debug.WriteLine(name + " not found");
                return false;
            }
        }

        public static bool ReserveRegister(this List<MemObj> vars, out int loc)
        {
            List<MemObj> registers = vars.FindAll(v => v.Location >= 112);
            registers.Sort((a, b) => a.LastModified.CompareTo(b.LastModified));
            List<MemObj> memory = vars.FindAll(v => v.Location <= 111);

            loc = vars.FindReg();
            if (loc != 0)
            {
                vars.Add(new MemObj("imm", 1, loc));
                return true;
            }
            else
            {
                if (vars.FindMem(out int memLoc))
                {
                    loc = registers.Last().Location;    // find oldest modified register
                    VariableManager.Code.Add("store r" + (loc - 112).ToString() + ", " + memLoc.ToString()); // "store regLoc memLoc"
                    vars.VarAt(loc).Location = memLoc;
                    vars.Add(new MemObj("imm", 1, loc));
                    return true;
                }
                else
                {
                    loc = 0;
                    Debug.WriteLine("memory full");
                    return false;   // memory is full
                }
            }
        }
    }

    public static class VariableManager
    {
        private static List<MemObj> Variables = new List<MemObj>();

        static List<Func<string, bool>> Handlers = new List<Func<string, bool>>()
        {
            //Match { byte a }
            delegate (string line)
            {
                Regex pattern = new Regex(@"^byte\s*([A-z]+)", RegexOptions.Compiled);
                Match match = pattern.Match(line);
                string varName = match.Groups[1].Value;
                if (pattern.IsMatch(line) && !Variables.Exists(varName))
                {
                    Variables.Add(new MemObj(varName, 1, 0) { Active = false });
                    Debug.WriteLine("var added: " + varName);
                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { a++ | a-- }
            delegate (string line)
            {
                Regex pattern = new Regex(@"^\s*([A-z]+)([+|-]{2})", RegexOptions.Compiled);
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:++/--
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], false))
                {
                    string opr = g[2] == "++" ? opr = "inc" : opr = "dec";
                    string aLoc = " r" + (Variables.Location(g[1]) - 112).ToString();
                    Code.Add(opr + aLoc);
                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = # }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = #");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:#
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], true))
                {
                    string aLoc = " r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    Code.Add("imm" + aLoc + g[2]);
                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = b }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = b");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:b
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], true) && Variables.Status(g[2]))
                {
                    string aLoc = " r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    int bLoc = Variables.Location(g[2]);
                    if (bLoc >= 112)
                    {
                        string bReg = "r" + (bLoc - 112).ToString();
                        Code.Add("and" + aLoc + bReg + ", " + bReg);
                    }
                    else
                    {
                        Code.Add("load" + aLoc + bLoc.ToString());
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = ~b }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = ~b");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:b
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], true) && Variables.CacheVariable(g[2], false))
                {
                    string aLoc = "r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string bLoc = "r" + (Variables.Location(g[2]) - 112).ToString();
                    Code.Add("inv " + aLoc + bLoc);
                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { a ? # }
            delegate (string line)
            {
                Regex pattern = MakeRegex("a ? #");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:?, g[3]:#
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], false) && Program.CmpOps.ContainsKey(g[2]) && Variables.ReserveRegister(out int literal))
                {
                    string aLoc = "r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string nLoc = "r" + (literal - 112).ToString();
                    string cmp = Program.CmpOps[g[2]] + " ";
                    Code.Add("imm " + nLoc + ", " + g[3]);
                    Code.Add(cmp + aLoc + nLoc);

                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { a ? b }
            delegate (string line)
            {
                Regex pattern = MakeRegex("a ? b");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:?, g[3]:b
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && Variables.CacheVariable(g[1], false) && Program.CmpOps.ContainsKey(g[2]) && Variables.CacheVariable(g[3], false))
                {
                    string aLoc = "r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string bLoc = "r" + (Variables.Location(g[3]) - 112).ToString();
                    string cmp = Program.CmpOps[g[2]] + " ";
                    Code.Add(cmp + aLoc + bLoc);

                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = # @ # }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = # @ #");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:#, g[3]:@, g[4]:#
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && !g.Contains("") && Variables.CacheVariable(g[1], true) && Variables.ReserveRegister(out int nA) && Program.AssignOps.ContainsKey(g[3]) && Variables.ReserveRegister(out int nB))
                {
                    string aLoc = "r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string naLoc = "r" + (nA - 112).ToString() + ", ";
                    string nbLoc = "r" + (nB - 112).ToString() + ", ";
                    string opr = Program.AssignOps[g[3]] + " ";
                    Code.Add("imm " + naLoc + g[2]);
                    Code.Add("imm " + nbLoc + g[4]);
                    Code.Add(opr + aLoc + naLoc + nbLoc);

                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = b @ # }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = b @ #");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:b, g[3]:@, g[4]:#
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && !g.Contains("") && Variables.CacheVariable(g[1], true) && Variables.CacheVariable(g[2], false) && Program.AssignOps.ContainsKey(g[3]) && Variables.ReserveRegister(out int n))
                {
                    string aLoc = "r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string bLoc = "r" + (Variables.Location(g[2]) - 112).ToString() + ", ";
                    string nLoc = "r" + (n - 112).ToString();
                    string opr = Program.AssignOps[g[3]] + " ";
                    Code.Add("imm " + nLoc + ", " + g[4]);
                    Code.Add(opr + aLoc + bLoc + nLoc);

                    return true;
                }
                else
                {
                    return false;
                }
            },

            //Match { (byte) a = b @ c }
            delegate (string line)
            {
                Regex pattern = MakeRegex("byte a = b @ c");
                Match match = pattern.Match(line);
                // g[1]:a, g[2]:b, g[3]:@, g[4]:c
                string[] g = match.Groups.Cast<Group>().Select(s => s.Value).ToArray();
                if (g[0] == line && !g.Contains("") && Variables.CacheVariable(g[1], true) && Variables.CacheVariable(g[2], false) && Program.AssignOps.ContainsKey(g[3]) && Variables.CacheVariable(g[4], false))
                {
                    string aLoc = " r" + (Variables.Location(g[1]) - 112).ToString() + ", ";
                    string bLoc = "r" + (Variables.Location(g[2]) - 112).ToString() + ", ";
                    string cLoc = "r" + (Variables.Location(g[4]) - 112).ToString();
                    string opr = Program.AssignOps[g[3]];
                    Code.Add(opr + aLoc + bLoc + cLoc);

                    return true;
                }
                else
                {
                    return false;
                }
            }
        };

        public static Dictionary<string, Regex> Expressions = new Dictionary<string, Regex>();
        public static List<string> Code;

        public static bool Handler(string line, out List<string> code)
        {
            Debug.WriteLine("\r\nline: " + line);
            Code = new List<string>();
            Handlers.ForEach(h => { h(line); });
            Variables.RemoveAll(v => v.Name == "imm");
            Variables.FindAll(v => v.Location >= 112).ForEach(v => v.LastModified++);

            Debug.WriteLine("Variables: ");
            Variables.ForEach(v => Debug.WriteLine(v.Name + "(" + v.LastModified + "): " + v.Location + "  " + v.Active));
            
            Debug.WriteLine("New Code: ");
            Code.ForEach(s => Debug.WriteLine(s));
            code = Code;
            return true;
        }

        public static Regex MakeRegex(string exp)
        {
            if (Expressions.ContainsKey(exp))
            {
                return Expressions[exp];
            }
            else
            {
                Dictionary<string, string> Translator = new Dictionary<string, string>()
                {
                    { "byte", "(?:byte)?\\s*" },
                    { "x", "([a-z]+)\\s*" },
                    { "#", "(\\S?[0-9abcdef]+)\\s*" },
                    { "=", "[=]\\s*" },
                    { "~x", "[~]" },
                    { "@", "(\\S+)\\s*" },
                    { "?", "(\\S+)\\s*" }
                };
                Regex letters = new Regex("[A-z]", RegexOptions.Compiled);

                string[] parts = exp.Split(' ');
                string pattern = "^\\s*";
                foreach (string s in parts)
                {
                    string s_r = Translator.ContainsKey(s) ? s : letters.Replace(s, "x");
                    pattern += Translator[s_r];
                }

                Expressions.Add(exp, new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
                return Expressions[exp];
            }
        }
    }
}