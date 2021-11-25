using System;
using System.Collections.Generic;

namespace Dylan_s_Machine_Lang_runtime
{
    class Program
    {
        static void Main(string[] args)
        {
            ShowWelcome();

        }

        static void ShowWelcome()
        {
            Console.WriteLine(@"
                                ██████╗  ███╗   ███╗ ██╗      ██████╗ 
                                ██╔══██╗ ████╗ ████║ ██║      ██╔══██╗
                                ██║  ██║ ██╔████╔██║ ██║      ██████╔╝
                                ██║  ██║ ██║╚██╔╝██║ ██║      ██╔══██╗
                                ██████╔╝ ██║ ╚═╝ ██║ ███████╗ ██║  ██║
                                ╚═════╝  ╚═╝     ╚═╝ ╚══════╝ ╚═╝  ╚═╝
                                   ");
            Console.WriteLine("                                Dylan's machine language runtime version 1.0.0");
            MachineLangAnalyser machineLangAnalyser = new MachineLangAnalyser();
            machineLangAnalyser.LoadIns(
                @"
2101
2202
5312
3303
D003
C000");
            machineLangAnalyser.ExecuteFrom(0);
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
        public void Satrt(int counter = 0)
        {
            isHalted = false;
            int step = 0;
            while (step++<MAX_STEP)
            {
                if (isHalted) return;
                Execute(Memory.MemGet(programCounter).IntToIns());
                ++programCounter;
            }
        }

        public void InitIns(string ins)
        {
            int addr = 0;
            foreach (var instructon in ins.Split('\r'))
            {
                if (instructon.Trim() == "") continue;
                Instructions.Enqueue(instructon.Trim());
                Memory.MemSet(addr++,instructon.HexToInt());
            }
            log($"Loaded instructions form MEM_0 to MEM_{addr}", LogLevel.info);
            programCounter = 0;
        }
        public void InitMem(string initCmd) 
        {
            string[] mems = initCmd.Split(';');
            foreach (var mem in mems)
            {
                string[] ss = mem.Split(',');
                int addr = ss[0].HexToInt();
                int val = ss[1].HexToInt();
                Memory.MemNew(addr, val);
            }
        }
        #region log
        static void log(string x, LogLevel level)
        {
            Console.WriteLine($"[{levelText[(int)level],10}]{x}");
        }
        static string[] levelText = { "fatal", "warning", "info" };
        enum LogLevel
        {
            fatal,
            warning,
            info,
        }

        #endregion
        private void Execute(string Instruction)
        {
            if (isHalted) return;
            char insType = Instruction[0];
            switch (insType)
            {
                case '1':
                    RunIns(Instruction, new Op1());
                    break;
                case '2':
                    RunIns(Instruction, new Op2());
                    break;
                case '3':
                    RunIns(Instruction, new Op3());
                    break;
                case '4':
                    RunIns(Instruction, new Op4());
                    break;
                case '5':
                    RunIns(Instruction, new Op5());
                    break;
                case '6':
                    RunIns(Instruction, new Op6());
                    break;
                case '7':
                    RunIns(Instruction, new Op7());
                    break;
                case '8':
                    RunIns(Instruction, new Op8());
                    break;
                case '9':
                    RunIns(Instruction, new Op9());
                    break;
                case 'A':
                    RunIns(Instruction, new OpA());
                    break;
                case 'B':
                    RunIns(Instruction, new OpB());
                    break;
                case 'C':
                    RunIns(Instruction, new OpC());
                    break;
                case 'D':
                    RunIns(Instruction, new OpD());
                    break;
                default:
                    break;
            }
        }
        private void RunIns(string ins,IOperation operation)
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
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS));
                log($"MOVE REG_{instruction.Substring(2, 1)} to REG_{instruction.Substring(3, 1)}", LogLevel.info);
                return true;
            }
        }
        class Op5 : IOperation
        {
            public string Format { get; set; } = "5 R S T";
            public bool execute(string instruction)
            {
                int regAddressR = instruction.Substring(1, 1).HexToInt();
                int regAddressS = instruction.Substring(2, 1).HexToInt();
                int regAddressT = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressR,Register.RegisterGet(regAddressS)+Register.RegisterGet(regAddressT));
                log($"ADD REG_{instruction.Substring(2, 1)} REG_{instruction.Substring(3, 1)},stored in REG_{instruction.Substring(1, 1)}", LogLevel.info);
                return true;
            }
        }
        class Op6 : IOperation
        {
            public string Format { get; set; } = "6 R S T";
            public bool execute(string instruction)
            {
                //TODO
                log($"ADD(float) REG_{instruction.Substring(1, 1)} REG_{instruction.Substring(2, 1)},stored in REG_{instruction.Substring(3, 1)}", LogLevel.info);

                return true;
            }
        }
        class Op7 : IOperation
        {
            public string Format { get; set; } = "7 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(1, 2).HexToInt();
                int regAddressT = instruction.Substring(2, 1).HexToInt();
                int regAddressR = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) | Register.RegisterGet(regAddressT));
                log($"OR REG_{instruction.Substring(1, 1)} and REG_{instruction.Substring(2, 1)},stored in REG_{instruction.Substring(3, 1)}", LogLevel.info);

                return true;
            }
        }
        class Op8 : IOperation
        {
            public string Format { get; set; } = "8 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(1, 2).HexToInt();
                int regAddressT = instruction.Substring(2, 1).HexToInt();
                int regAddressR = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) & Register.RegisterGet(regAddressT));
                log($"AND REG_{instruction.Substring(1, 1)} and REG_{instruction.Substring(2, 1)},stored in REG_{instruction.Substring(3, 1)}", LogLevel.info);

                return true;
            }
        }
        class Op9 : IOperation
        {
            public string Format { get; set; } = "9 R S T";
            public bool execute(string instruction)
            {
                int regAddressS = instruction.Substring(1, 2).HexToInt();
                int regAddressT = instruction.Substring(2, 1).HexToInt();
                int regAddressR = instruction.Substring(3, 1).HexToInt();
                Register.RegisterSet(regAddressR, Register.RegisterGet(regAddressS) ^ Register.RegisterGet(regAddressT));
                log($"EXCLUSIVE REG_{instruction.Substring(1, 1)} and REG_{instruction.Substring(2, 1)},stored in REG_{instruction.Substring(3, 1)}", LogLevel.info);

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

            int move_right(int val,int n)
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
                int regAddressR = instruction.Substring(1, 2).HexToInt();
                int memAddressXY = instruction.Substring(2, 2).HexToInt();
                if (Register.RegisterGet(0)==Register.RegisterGet(regAddressR))
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
            public string Format { get; set; } = "D 0 XY";
            public bool execute(string instruction)
            {
                log($"MEM_{instruction.Substring(2,2)} = {Memory.MemGet(instruction.Substring(2,2).HexToInt())}", LogLevel.fatal);
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
        void log(string x, LogLevel level)
        {
            Console.WriteLine($"[{levelText[(int)level],10}]{x}");
        }
        string[] levelText = { "fatal", "warning", "info" };
        enum LogLevel
        {
            fatal,
            warning,
            info,
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
        string[] levelText = { "fatal", "warning", "info" };
        enum LogLevel
        {
            fatal,
            warning,
            info,
        }
        void log(string x, LogLevel level)
        {
            Console.WriteLine($"[{levelText[(int)level],10}]{x}");
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
                    log($"Try access uninitalized memory @{address}", LogLevel.warning);
                    return 0;
                }
            }
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
    }
}
