using System;
using System.Collections.Generic;
using System.IO;

namespace Dylan_s_Machine_Lang_runtime
{
    class Program
    {
        static void
        Main(string[] args)
        {
            ShowWelcome();
            string cmdPath = "";
            if (args.Length > 0)
            {
                cmdPath = args[0];
            }
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("===Guide Line===");
            Console.WriteLine("If the address start from 00, DMLR will initialize memory form 00");
            Console.WriteLine("Or the address doesn't start from 00, You shall input a formated text like this:");
            Console.WriteLine("F0,20;F1,00;F2,22;F3;02;");
            Console.WriteLine("it should be:  \nMemory_address,Contents;\n (split them by ',') ");
            Console.WriteLine("and split the whole address-content unit by ';'");
            Console.WriteLine("In which the enter and space will be ignored.");
            Console.WriteLine("The simple format should go like this:");
            Console.WriteLine("2211\n3202\nC000");
            Console.WriteLine("Every line a character instruction.");
            Console.WriteLine();
            Console.WriteLine();
        restart:
            Console.WriteLine("Please Input a txt file (Relative path e.g. t.txt or absloute path C:\\Users\\Administrator\\Desktop):");
            while (true)
            {
                cmdPath = Console.ReadLine().Trim();
                if (File.Exists(cmdPath))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Unable to access file" + cmdPath + ",Please try again.");
                }
            }
            string text = File.ReadAllText(cmdPath);
            MachineLangAnalyser machineLangAnalyser = new MachineLangAnalyser();
            if (text.Contains(','))
            {
                Console.WriteLine("Loading Complex Memory format file...");
                Console.WriteLine();
                machineLangAnalyser.LoadMem(text);
                Console.WriteLine("Input the HEX memory address entry(where the program counter starts):");
                machineLangAnalyser.ExecuteFrom(Console.ReadLine().Trim().HexToInt());
            }
            else
            {
                Console.WriteLine("Loading Simple Memory format file...");
                Console.WriteLine();
                machineLangAnalyser.LoadIns(text);
                machineLangAnalyser.ExecuteFrom(0);
            }
            Console.WriteLine("Enter exit to exit,press enter to continue.");
            if (Console.ReadLine().Trim() == "exit")
            {
                //stop
            }
            else
            {
                Console.WriteLine("Reinitializing Virtual Machine...");
                machineLangAnalyser.Initiialize();
                goto restart;
            }
        }

        static void ShowWelcome()
        {
            Console.WriteLine(@"
                                ███████╗ ███╗   ███╗ ██╗      ██████╗ 
                                ██╔════╝ ████╗ ████║ ██║      ██╔══██╗
                                ███████╗ ██╔████╔██║ ██║      ██████╔╝
                                ╚════██║ ██║╚██╔╝██║ ██║      ██╔══██╗
                                ███████║ ██║ ╚═╝ ██║ ███████╗ ██║  ██║
                                ╚══════╝ ╚═╝     ╚═╝ ╚══════╝ ╚═╝  ╚═╝
                                   
                                   ");
            Console.WriteLine("                          bk's machine language runtime version 1.1.0");

        }
    }

    class MachineLangAnalyser
    {
        public MachineLangAnalyser() { }
        public MachineLangAnalyser(string[] commmand)
        {

        }
        MLCPU MLCPU = new MLCPU();
        public void LoadMem(string mem)
        {
            MLCPU.InitMem(mem);
        }
        public void LoadIns(string ins)
        {
            MLCPU.InitIns(ins.Trim());
        }
        public void ExecuteFrom(int counter)
        {
            MLCPU.Satrt(counter);
        }

        public void Initiialize()
        {
            MLCPU.Initiialize();
        }
    }

    class MLCPU
    {
        public static bool AllowUnsafeOperations = false;

        static MLREG Register = new MLREG();
        static MLMEM Memory = new MLMEM();
        static Queue<string> Instructions = new Queue<string>();
        static int programCounter;
        static bool isHalted = false;
        static int MAX_STEP = 500;
        Queue<(int, string)> UserCMD = new Queue<(int, string)>();
        public void Satrt(int counter = 0)
        {
            programCounter = counter;
            Logger.pc = programCounter;

            isHalted = false;
            int step = 0;
            while (step++ < MAX_STEP)
            {
                //perform usercommand
                (int, string) userCmd;
                if (UserCMD.Count > 0)
                {
                    UserCMD.TryPeek(out userCmd);
                    if (userCmd.Item1 <= programCounter)
                    {
                        while (UserCMD.Count > 0)
                        {
                            UserCMD.TryPeek(out userCmd);
                            if (userCmd.Item1 <= programCounter)
                            {
                                Execute(userCmd.Item2);
                                UserCMD.Dequeue();
                            }
                            else
                                break;
                        }
                    }
                }
                Execute(Memory.MemGet(programCounter++).IntTo2Hex() + Memory.MemGet(programCounter++).IntTo2Hex());
                Logger.pc = programCounter;
                if (isHalted) return;
            }
            log("ERROR: TOO MANY EXECUTIONS!", LogLevel.fatal);
        }
        public void Initiialize()
        {
            Register.Initiialize();
            Memory.Initiialize();
            Instructions.Clear();
            UserCMD.Clear();
        }
        public void InitIns(string ins)
        {
            int addr = 0;
            foreach (var instruction in ins.Split('\r'))
            {
                string t = instruction.Trim();
                if (t[0] == '#' || t == "") continue;
                if (t[0] == '$')
                {
                    //is an command
                    UserCMD.Enqueue((addr, t[1..]));
                    continue;
                }
                string s = instruction.Trim().Substring(0, 4);
                Instructions.Enqueue(s);
                Memory.MemSet(addr++, s.Substring(0, 2).HexToInt());
                Memory.MemSet(addr++, s.Substring(2, 2).HexToInt());
            }
            log($"Loaded instructions form MEM_00 to MEM_{addr.IntTo2Hex()}", LogLevel.info);
            programCounter = 0;
        }
        public void InitMem(string initCmd)
        {
            string[] mems = initCmd.Split('\n');
            int _tmp_addr = 0;
            foreach (var mem in mems)
            {
                string s = mem.Trim();
                if (s == "" || s[0] == '#') continue;
                if (s[0] == '$')
                {
                    UserCMD.Enqueue((_tmp_addr, s[1..]));
                    continue;
                }
                string[] ss = s.Split(',');
                int addr = ss[0].HexToInt();
                _tmp_addr = addr;
                int val = ss[1].HexToInt();
                Memory.MemNew(addr, val);
            }
            log($"Loaded customized instructions till {_tmp_addr.IntTo2Hex()}", LogLevel.info);

        }
        #region log
        static void log(string x, LogLevel level)
        {
            Logger.log(x, level);
        }
        #endregion

        static IOperation[] operations = { new Op1(), new Op2(), new Op3(), new Op4(), new Op5(), new Op6(), new Op7(), new Op8(), new Op9(), new OpA(), new OpB(), new OpC(), new OpD(), new OpE(), new OpF(), new __UNKNOWN() };
        private void Execute(string Instruction)
        {
            if (isHalted) return;
            int insType = Instruction[0].ToString().HexToInt();
            if (insType > 0 && insType < 16)
            {
                RunIns(Instruction, operations[insType - 1]);
            }
            else
            {
                RunIns(Instruction, new __UNKNOWN());
            }
        }
        private void RunIns(string ins, IOperation operation)
        {
            operation.execute(ins);
        }
        interface IOperation
        {
            public string Format { get; set; }
            bool execute(string instruction);
        }
        class Op1 : IOperation
        {
            public string Format { get; set; } = "1 R XY";
            public bool execute(string instruction)
            {
                int memAddress = instruction.Substring(2, 2).HexToInt();
                int regAddress = instruction.Substring(1, 1).HexToInt();
                Register.RegisterSet(regAddress, Memory.MemGet(memAddress));
                log($"LOAD MEM_{instruction.Substring(2, 2)} to REG_{instruction.Substring(1, 1)}", LogLevel.info);
                return true;
            }
        }
        class Op2 : IOperation
        {
            public string Format { get; set; } = "2 R XY";
            public bool execute(string instruction)
            {
                int value = instruction.Substring(2, 2).HexToInt();
                int regAddress = instruction.Substring(1, 1).HexToInt();
                Register.RegisterSet(regAddress, value);
                log($"LOAD {instruction.Substring(2, 2)} to REG_{instruction.Substring(1, 1)}", LogLevel.info);
                return true;
            }
        }
        class Op3 : IOperation
        {
            public string Format { get; set; } = "3 R XY";
            public bool execute(string instruction)
            {
                int memAddress = instruction.Substring(2, 2).HexToInt();
                int regAddress = instruction.Substring(1, 1).HexToInt();
                Memory.MemSet(memAddress, Register.RegisterGet(regAddress));
                log($"STORE REG_{instruction.Substring(1, 1)} to MEM_{instruction.Substring(2, 2)}", LogLevel.info);
                return true;
            }
        }
        class Op4 : IOperation
        {
            public string Format { get; set; } = "4 0 RS";
            public bool execute(string instruction)
            {
                int regAddressR = instruction.Substring(2, 1).HexToInt();
                int regAddressS = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressS, Register.RegisterGet(regAddressR));
                log($"MOVE REG_{instruction.Substring(2, 1)} to REG_{instruction.Substring(3, 1)}", LogLevel.info);
                return true;
            }
        }
        /// <summary>
        /// ADD in 2's completement
        /// </summary>
        class Op5 : IOperation
        {
            public string Format { get; set; } = "5 R S T";
            public bool execute(string instruction)
            {
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                int regAddressS = instruction.Substring(2, 1).HexToInt();
                int regAddressT = instruction.Substring(3, 1).HexToInt();
                int ans = Register.RegisterGet(regAddressS) + Register.RegisterGet(regAddressT);
                if (ans >= (1 << 8))
                {
                    Register.RegisterSet(regAddressR, ans & 0b1_0000_0000);
                    log($"ADD: REG_{instruction.Substring(1, 1)}=REG_{instruction.Substring(2, 1)}+REG_{instruction.Substring(3, 1)}={ans & 0b1_0000_0000:x}(Overflowed)", LogLevel.info);
                }
                else
                {
                    Register.RegisterSet(regAddressR, ans);
                    log($"ADD REG_{instruction.Substring(2, 1)} REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);
                }

                return true;
            }
        }
        class Op6 : IOperation
        {
            public string Format { get; set; } = "6 R S T";
            public bool execute(string instruction)
            {
                int addr_S = instruction.Substring(2, 1).HexToInt();
                int addr_T = instruction.Substring(3, 1).HexToInt();
                int addr_R = instruction.Substring(1, 1).HexToInt();
                log($"ADD(float) REG_{instruction.Substring(2, 1)} REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);
                double rS = Int2float(Register.RegisterGet(addr_S));
                double rT = Int2float(Register.RegisterGet(addr_T));
                double ans = rS + rT;
                unsafe
                {
                    int offset = (1 << 10) - 1;
                    long value = GetMemoryValue(ans);
                    int sign = (int)(((ulong)value & 0x8000000000000000) >> 63);
                    int e = (int)((value & 0x7FF0000000000000) >> 52) - offset;
                    int e2 = (1 << 2) + e + 1;
                    long d = (value & 0xFFFFFFFFFFFFF);
                    ulong t = (GetBinay(GetBaseNumber(d)));
                    int firstOneInt = 3;
                    for (int i = 64 - 1; i >= 0; i--)
                    {
                        if ((t & (0b1UL << i)) > 0)
                        {
                            firstOneInt = i;
                            break;
                        }
                    }
                    if (firstOneInt > 3)
                    {
                        t >>= firstOneInt - 3;
                    }
                    else if (firstOneInt < 3)
                    {
                        t <<= 3 - firstOneInt;
                    }
                    int myEigttBitFloat = 0;
                    if (firstOneInt > 3)
                    {
                        log($"float operation truncation error", LogLevel.warning);
                    }
                    else
                    {
                        //Nothing
                    }
                    myEigttBitFloat = (sign << 7) | (e2 << 4) | ((int)t);
                    //Console.WriteLine(myEigttBitFloat);
                    Register.RegisterSet(addr_R, myEigttBitFloat);
                }
                return true;
            }

            private double Int2float(int i)
            {
                int sign = (i & 0b1000_0000) == 0 ? 1 : -1;
                int exceed = ((i & 0b0111_0000) >> 4) - 0b0100;
                double t = 0;
                int digits = i & 0b0000_1111; double weight = 1d / 16d;
                for (int j = 0; j < 4; j++)
                {
                    t += ((digits & (1 << j)) > 0 ? 1 : 0) * weight;
                    weight *= 2;
                }
                return sign * t * Math.Pow(2, exceed);
            }

            static unsafe long GetMemoryValue(double value)
            {
                return (*((long*)(&value)));
            }
            static double GetBaseNumber(long value)
            {
                double result = 0.0, u = 0x10000000000000;
                long d = 0x1;
                for (int i = 0; i < 52; i++)
                {
                    result += ((value & d) >> i) / u;
                    d <<= 1;
                    u /= 2;
                }
                return 1 + result;
            }

            static ulong GetBinay(double d)
            {
                double b = 1;
                int counter = 63;
                ulong ans = 0;
                while (d > 0 && b > 1d / (1L << 31))
                {
                    if (d >= b)
                    {
                        ans |= 1uL << counter;
                        d -= b;
                        //Console.WriteLine("+"+b);
                    }
                    b /= 2f;
                    --counter;
                }
                int cnt = 0;
                while ((ans & 0b1) == 0 && cnt < 64)
                {
                    ans >>= 1;
                    ++cnt;
                }
                return ans;
            }
        }
        class Op7 : IOperation
        {
            public string Format { get; set; } = "7 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(2, 1).HexToInt();
                int regAddressT = instruction.Substring(3, 1).HexToInt();
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) | Register.RegisterGet(regAddressT));
                log($"OR REG_{instruction.Substring(2, 1)} and REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);

                return true;
            }
        }
        class Op8 : IOperation
        {
            public string Format { get; set; } = "8 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(2, 1).HexToInt();
                int regAddressT = instruction.Substring(3, 1).HexToInt();
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) & Register.RegisterGet(regAddressT));
                log($"AND REG_{instruction.Substring(2, 1)} and REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);

                return true;
            }
        }
        class Op9 : IOperation
        {
            public string Format { get; set; } = "9 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(2, 1).HexToInt();
                int regAddressT = instruction.Substring(3, 1).HexToInt();
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) ^ Register.RegisterGet(regAddressT));
                log($"EXCLUSIVE REG_{instruction.Substring(2, 1)} and REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);

                return true;
            }
        }
        class OpA : IOperation
        {
            public string Format { get; set; } = "9 R 0 X";
            public bool execute(string instruction)
            {
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                int times = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressR, move_right(Register.RegisterGet(regAddressR), times));
                log($"ROTATE REG_{instruction.Substring(1, 1)} {instruction.Substring(3, 1)} times", LogLevel.info);

                return true;
            }

            int move_right(int val, int n)
            {
                int N = 32;
                return (val >> (N - n) | (val << n));
            }
        }
        class OpB : IOperation
        {
            public string Format { get; set; } = "B R XY";
            public bool execute(string instruction)
            {
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                int memAddressXY = instruction.Substring(2, 2).HexToInt();
                if (Register.RegisterGet(0) == Register.RegisterGet(regAddressR))
                {
                    //Jump
                    programCounter = memAddressXY;
                    log($"JUMP to MEM_{instruction.Substring(2, 2)}", LogLevel.info);
                }
                else
                {
                    log($"JUMP but failed, continue at MEM_{programCounter.ToString("X2")}", LogLevel.info);
                    //DoNothing
                }
                return true;
            }
        }
        class OpC : IOperation
        {
            public string Format { get; set; } = "C 000";
            public bool execute(string instruction)
            {
                isHalted = true;
                log($"HALT at MEM_{programCounter.ToString("X2")}", LogLevel.info);
                return true;
            }
        }
        class OpD : IOperation
        {
            public string Format { get; set; } = "D _ XY";
            public bool execute(string instruction)
            {
                switch (instruction[1])
                {
                    case '0':
                        log($"MEM_{instruction.Substring(2, 2)} = {Memory.MemGet(instruction.Substring(2, 2).HexToInt()):x}", LogLevel.alert);
                        break;
                    case '1':
                        log($"REG_{instruction.Substring(3, 1)} = {Register.RegisterGet(instruction.Substring(3, 1).HexToInt()):x}", LogLevel.alert);
                        break;
                    case '2':
                        Memory.PrintAll();
                        break;
                    case '3':
                        Register.PrintAll();
                        break;
                    case '4':
                        log($"Program counter = {programCounter}", LogLevel.alert);
                        break;
                    default:
                        break;
                }
                return true;
            }


        }
        class OpE : IOperation
        {
            public string Format { get; set; } = "E";
            public bool execute(string instruction)
            {
                return true;
            }
        }
        class OpF : IOperation
        {
            public string Format { get; set; } = "F";
            public bool execute(string instruction)
            {
                return true;
            }
        }
        class __UNKNOWN : IOperation
        {
            public string Format { get; set; } = "0000";
            public bool execute(string instruction)
            {
                log($"UNKNOWN OPERATION!stack trace back:\n{instruction}@#{programCounter - 2:X}", LogLevel.fatal);
                Console.WriteLine("If you insist to continue, enter Y(y),or the programe will be enforced HALT by SMLR");
                string s = Console.ReadKey().KeyChar.ToString();
                if (s.ToUpper() == "Y")
                {
                    Console.WriteLine();
                    log($"Manually jumpped unsolved instruction @MEM_{programCounter - 2:X2}", LogLevel.warning);
                }
                else
                {
                    isHalted = true;
                    Console.WriteLine();
                    log($"Automatically HALT at MEM_{programCounter - 2:X2}", LogLevel.warning);
                    return true;
                }
                return true;
            }
        }
    }
    class MLREG
    {
        public static bool AllowUnsafeOperations = true;

        class Register
        {
            public int Value { get; set; }
            public int Address { get; set; }
        }
        static List<Register> _registers = new List<Register>();
        static Dictionary<int, Register> registers = new Dictionary<int, Register>();
        static void log(string x, LogLevel level)
        {
            Logger.log(x, level);
        }
        public void RegisterSet(int address, int value)
        {
            if (registers.ContainsKey(address))
            {
                registers[address].Value = value;
            }
            else
            {
                RegisterNew(address, value);
            }
        }
        public void RegisterNew(int address, int value)
        {
            Register register = new Register();
            register.Address = address;
            register.Value = value;
            _registers.Add(register);
            registers.Add(address, register);
        }
        public int RegisterGet(int address)
        {
            if (registers.ContainsKey(address))
            {
                return registers[address].Value;
            }
            else
            {
                if (AllowUnsafeOperations)
                {
                    return 0;
                }
                else
                {
                    log("Try access uninitalized register", LogLevel.warning);
                    return 0;
                }
            }
        }
        public void PrintAll()
        {
            log($"===START REGISTER SNAPSHOT===", LogLevel.alert);

            foreach (var item in _registers)
            {
                log($"   REG_{item.Address.IntTo2Hex()} = {item.Value.IntTo2Hex()}", LogLevel.alert);
            }
            log($"===END REGISTER SNAPSHOT===", LogLevel.alert);

        }
        public void Initiialize()
        {
            _registers.Clear();
            registers.Clear();
        }
    }
    class MLMEM
    {
        public static bool AllowUnsafeOperations = false;

        class MemCell
        {
            public int Value { get; set; }
            public int Address { get; set; }
        }
        private List<MemCell> _mem = new List<MemCell>();
        private Dictionary<int, MemCell> mem = new Dictionary<int, MemCell>();
        static void log(string x, LogLevel level)
        {
            Logger.log(x, level);
        }
        public void MemSet(int address, int value)
        {
            if (mem.ContainsKey(address))
            {
                mem[address].Value = value;
            }
            else
            {
                MemNew(address, value);
            }
        }
        public void MemNew(int address, int value)
        {
            MemCell memCell = new MemCell();
            memCell.Address = address;
            memCell.Value = value;
            _mem.Add(memCell);
            mem.Add(address, memCell);
        }
        public int MemGet(int address)
        {
            if (mem.ContainsKey(address))
            {
                return mem[address].Value;
            }
            else
            {
                if (AllowUnsafeOperations)
                {
                    return 0;
                }
                else
                {
                    log($"Try access uninitalized memory @{address:X}", LogLevel.warning);
                    return 0;
                }
            }
        }
        public void PrintAll()
        {
            log("===START MEMORY SNAPSHOT===", LogLevel.alert);
            foreach (var item in _mem)
            {
                log($"   MEM_{item.Address.IntTo2Hex()} = {item.Value.IntTo2Hex()}", LogLevel.alert);
            }
            log($"===END MEMORY SNAPSHOT===", LogLevel.alert);
        }
        public void Initiialize()
        {
            _mem.Clear();
            mem.Clear();
        }
    }
    public static class Helper
    {
        public static int HexToInt(this string s)
        {
            return Convert.ToInt32(s.Trim(), 16);
        }


        public static string IntToIns(this int s)
        {
            return s.ToString("X4");
        }

        public static string IntTo2Hex(this int s)
        {
            return s.ToString("X2");
        }
    }
    public enum LogLevel
    {
        fatal,
        warning,
        info,
        alert,
    }
    public static class Logger
    {
        public static int pc = 0;
        static string[] levelText = { "fatal", "warning", "info", "alert" };

        public static void log(string x, LogLevel level)
        {
            Console.WriteLine($"#{pc,-3:X}[{levelText[(int)level] + ']',-10}  {x}");
        }
    }
}
