using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace McGuire1Sim
{
    /// <summary>
    /* McGuire 1.0 CPU Simulator
    * by: Steven Lenig, Stephanie Carter, Sergio Rocha, Seth Bane
    *
    * Purpose: This program simulates the operation of a simple 16-bit Processor
    *			OPCODE  |   DMODE |  DREG   |  SMODE  |  SREG
    *        00 01 02 03| 04 05 06| 07 08 09| 10 11 12| 13 14 15 
    *         0  0  0  0|  0  0  0|  0  0  0|  0  0  0|  0  0  0
    *    
    *       OPCODE
    *       MOV   0000           BRA   1000
    *       NOT   0001           BEQ   1001
    *       ADD   0010           BNE   1010
    *       SUB   0011           BGT   1011
    *       OR    0100           BLT   1100
    *       AND   0101           BRZ   1101
    *       CMP   0110           SHL   1110
    *       XOR   0111           SHR   1111
    *
    *        MODE
    *        REG DIRECT                D0      000  
    *        REG INDIRECT             (D0)     001
    *        AUTO-INC                 (D0)+    010
    *        AUTO-INC DEFERRED       @(D0)+    011
    *        AUTO-DEC                -(D0)     100d
    *        AUTO-DEC DEFERRED      @-(D0)     101
    *
    *        LITERAL                   ##      111 ; LIT - > SMODE + SREG bits
 
    */
    /// </summary>
    public partial class Window1 : Window
    {
        short []memory;				//Main memory implemented as array
        short []reg ;						//Array of registers
        short PC = 0,MAR = 0, MBR = 0, IR = 0;
        short []CCR; //Status register sign || zero || overflow || unsigned overflow
        short opcode = 0,smode = 0,dmode = 0,src = 0,dest = 0;
        short source = 0,destination = 0;			//These are the ALU operands
        Window2 M1_Memory = new Window2();
        short line_number;

        public Window1()
        {
            InitializeComponent();
            memory =  new short[256];				//Main memory implemented as array
            reg = new short[8];						//Array of registers     
            CCR = new short[4]; //Status register sign || zero || overflow || unsigned overflow    
        }


        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {

            short current_word = 0x0;
            line_number = 0;

            PC = MAR = MBR = IR = 0;
            CCR[0] = CCR[1] = CCR[2] = CCR[3] = 0;

            string instr_line="";
            string inFileName;
            string buffer ="";
            System.IO.StreamReader asm_reader;
            System.IO.StreamReader instr_reader;         //next line forces crash on non-Windows platforms
            Microsoft.Win32.OpenFileDialog openf = new Microsoft.Win32.OpenFileDialog();
            openf.FileName = "M1ASM.txt";
            openf.DefaultExt = ".txt";
            openf.Filter = "Text documents (.txt)|*.txt";
            openf.InitialDirectory = ".";

            Nullable<bool> is_open = openf.ShowDialog();

            if (is_open == true)
            {
                textBoxMain.Text = "";

                for (int i = 0; i < 256; ++i)
                    memory[i] = 0;

                for (int i = 0; i < 8; ++i)
                    reg[i] = 0;

                inFileName = openf.FileName;
                asm_reader = new System.IO.StreamReader(inFileName);

                System.Diagnostics.Process compile = new System.Diagnostics.Process();
                compile.StartInfo.FileName = "McGuire_Compiler.exe";

                inFileName = "\"" + inFileName + "\"";
                inFileName += "\0";
                compile.StartInfo.Arguments = inFileName;
                compile.StartInfo.UseShellExecute = true;

                compile.Start();
                compile.WaitForExit();     //wait for it to exit before reading instructions
   
                instr_reader = new System.IO.StreamReader("m1out.txt");
                
                while ( !instr_reader.EndOfStream )
                {
                    instr_line = instr_reader.ReadLine();
                    buffer = String.Format("{0:X3}", current_word);
                    buffer += "     ";
                    buffer += instr_line;
                    buffer += "       ";
                    for (int i = instr_line.Length; i < 9; ++i)
                        buffer += " ";
                    buffer += String.Format("{0:D}", line_number);
                    buffer += "    ";
                    if (line_number < 10)
                        buffer += " ";
                    if (line_number < 100)
                        buffer += " ";
                    buffer += asm_reader.ReadLine();

                    current_word += 4;
                    ++line_number;

                    textBoxMain.Text += buffer;
                    textBoxMain.Text += '\n';
                }

                ButtonStep.IsEnabled = true;
                ButtonMem.IsEnabled = true;
                ButtonRun.IsEnabled = true;
                textBoxSteps.IsEnabled = true;

                loadProgram();

                instr_reader = new System.IO.StreamReader("m1errors.txt");

                if (instr_reader.EndOfStream)
                    textBoxMain.Text += "\nNo errors detected.";
                else
                    textBoxMain.Text += "\nErrors:";

                while (!instr_reader.EndOfStream)
                {
                    textBoxMain.Text += "\n   " + instr_reader.ReadLine();
                }

                instr_reader = new System.IO.StreamReader("m1symtbl.txt");

                if (!instr_reader.EndOfStream)
                {
                    textBoxMain.Text += "\n\n\n";
                }
                while (!instr_reader.EndOfStream)
                {
                    textBoxMain.Text += instr_reader.ReadLine() + "\n";
                }

                // add shl shr opcodes
            }
        }

        void fetch()				//Fetch the instruction.
        {
            MAR = PC;
            PC = (short)(PC + 1);
            MBR = memory[MAR];
            IR = MBR;

            opcode = (short)((IR & 0xF000) >> 12);	// Decode the opcode 
            smode  = (short)((IR & 0x0038) >> 3);	// and the modes.
            dmode  = (short)((IR & 0x0E00) >> 9);
            src    = (short)( IR & 0x0007);			//Get source register/Memory code
            dest   = (short)((IR & 0x01C0) >> 6);
        }


        void execute()
        {
            //Execute any branches first
            switch (opcode)
            {
                case 8: //Unconditional Branch
                    PC = (short)(IR & 0xFFF);
                    break;
                case 9: //Branch if Equal
                    if (CCR[1] == 1) PC = (short)(IR & 0xFFF);
                    break;
                case 10: //Branch if not Equal
                    if (CCR[1] == 0) PC = (short)(IR & 0xFFF);
                    break;
                case 11: //Branch if greater than
                    if (CCR[0] == 0) PC = (short)(IR & 0xFFF);
                    break;
                case 12: //Branch if less than
                    if (CCR[0] == 1) PC = (short)(IR & 0xFFF);
                    break;
                case 13: //Branch if Zero
                    if (CCR[1] == 1) PC = (short)(IR & 0xFFF);
                    break;
                default:
                    //Fetch the Destination operand
                    switch (dmode)
                    {
                        case 0:
                            destination = reg[dest];
                            break;
                        case 1:                       //Register indirect mode, the register contains the address of the data
                        case 2:                       //Register Indirect with auto-increment
                            destination = memory[reg[dest]];
                            break;
                        case 3:                        //Auto-Increment Deferred
                            destination = memory[memory[reg[dest]]];
                            break;
                        case 4:                        //Auto-Decrement
                            destination = memory[reg[dest]];
                            break;
                        case 5:                        //Auto-Decrement Deferred
                            destination = memory[memory[reg[dest]]];
                            break;
                        case 6:                        //Lead Index Addressing
                            //destination = memory[reg[dest] + reg[6]];
                            break;
                        case 7:                        //Loads a Literal	   
                            destination = (short)(IR & 0x3F);
                            smode = 8;
                            break;
                    }
                    switch (smode)          //Source Mode
                    {
                        case 0:
                            source = reg[src];        //Register Direct mode, change src to appropriate variable
                            break;
                        case 1:                       //Register indirect mode, the register contains the address of the data
                            source = memory[reg[src]];  //MEM implemented as an array for now, this can be changed later
                            break;
                        case 2:                       //Register Indirect with auto-increment
                            source = memory[reg[src]];
                            reg[src] += 2;            //Increment the register pointer by size of the operand type
                            break;
                        case 3:                        //Auto-Increment Deferred
                            source = memory[memory[reg[src]]];
                            reg[src] += 2;
                            break;
                        case 4:                        //Auto-Decrement
                            reg[src] -= 2;
                            source = memory[reg[src]];
                            break;
                        case 5:                        //Auto-Decrement Deferred
                            reg[src] -= 2;
                            source = memory[memory[reg[src]]];
                            break;
                        case 6:                        //Lead Index Addressing
                            //source = memory[reg[src] + reg[6]];
                            break;
                        case 7:                        //Index Deferred	   
                            //source = memory[memory[reg[src] + reg[6]]];
                            break;
                        default:
                            break;
                    }


                    //Have ALU perform the operation, but first clear CCR
                    CCR[0] = 0;
                    CCR[1] = 0;
                    CCR[2] = 0;
                    CCR[3] = 0;
                    switch (opcode)
                    {
                        case 0:                        //Move
                            if (dmode != 7)
                            {
                                destination = source;
                                break;
                            }
                            break;
                        case 1:                        //Not
                            destination = (short)(~destination & 077);
                            break;
                        case 6:                        //Compare
                            source *= -1;
                            destination += source;
                            break;
                        case 3:                        //Subract
                            source *= -1;
                            destination += source;
                            break;
                        case 2:                        //Add
                            destination += source;
                            break;
                        case 4:                        //Or
                            destination = (short)(source | destination);
                            break;
                        case 5:                        //And
                            destination = (short)(source & destination);
                            break;
                        case 7:                        //XOR
                            destination = (short)(source ^ destination);
                            break;
                        case 14:                       //SHL
                            destination = (short)(destination << source);
                            break;
                        case 15:                       //SHR
                            destination = (short)(destination >> source);
                            break;
                    }
                    //Set flags
                    if (destination < 0)
                    {
                        CCR[0] = 1;
                    }
                    if (destination == 0)
                    {
                        CCR[1] = 1;
                    }
                    if (destination > 65535)
                    {
                        CCR[2] = 1;
                    }
                    if (destination > 65535)
                    {
                        CCR[3] = 1;
                    }

                    //Store the result of the operation based on destination address mode
                    switch (dmode)
                    {
                        case 0:                           //Register Direct
                            reg[dest] = destination;
                            break;
                        case 1:                           //Register Indirect
                            memory[reg[dest]] = destination;
                            break;
                        case 2:                           //Auto-Increment
                            memory[reg[dest]] = destination;
                            reg[dest] += 1;
                            break;
                        case 3:                           //Auto-Increment Deferred
                            memory[reg[dest]] = destination;
                            reg[dest] += 2;
                            break;
                        case 4:                           //Auto-Decrement
                            reg[dest] -= 1;
                            memory[reg[dest]] = destination;
                            break;
                        case 5:                           //Auto-Decrement Deferred
                            reg[dest] -= 2;
                            memory[reg[dest]] = destination;
                            break;
                        case 6:                           //Lead Index Addressing
                            memory[reg[dest] + reg[6]] = destination;
                            break;
                        case 7:                           //Load the Literal
                            reg[dest] = destination;
                            break;
                    }
                    break;
            }
        }


        void loadProgram()
        {
            char []tchar;
            System.IO.StreamReader instr_reader = new System.IO.StreamReader("m1out.txt");
 
            int loopCtr = 0;

            tchar = new char [1];     //fgetc(inFile);
            while ( !instr_reader.EndOfStream)
            {
                instr_reader.Read(tchar, 0,1);
                if ((tchar[0] <= 'F' && tchar[0] >= 'A'))
                {
                    memory[loopCtr] = (short)(memory[loopCtr] << 4);
                    memory[loopCtr] += (short)(10 + tchar[0] - 'A');
                }
                else if (tchar[0] >= '0' && tchar[0] <= '9')
                {
                    memory[loopCtr] = (short)(memory[loopCtr] << 4);
                    memory[loopCtr] += (short)(tchar[0] - '0');

                }
                else if (tchar[0] == '\n' || instr_reader.EndOfStream)
                {
                    ++loopCtr;
                }
            }
        }


        void updateRegisters()
        {
            textBoxD0.Text = String.Format("{0:X}", reg[0]);
            textBoxD1.Text = String.Format("{0:X}", reg[1]);
            textBoxD2.Text = String.Format("{0:X}", reg[2]);
            textBoxD3.Text = String.Format("{0:X}", reg[3]);
            textBoxD4.Text = String.Format("{0:X}", reg[4]);
            textBoxD5.Text = String.Format("{0:X}", reg[5]);
            textBoxD6.Text = String.Format("{0:X}", reg[6]);
            textBoxD7.Text = String.Format("{0:X}", reg[7]);

            textBoxIR.Text  = String.Format("{0:X}", IR);
            textBoxPC.Text  = String.Format("{0:X}", PC);
            textBoxMAR.Text = String.Format("{0:X}", MAR);
            textBoxMBR.Text = String.Format("{0:X}", MBR);

            textBoxSF.Text = String.Format("{0:X}", CCR[0]);
            textBoxZF.Text = String.Format("{0:X}", CCR[1]);
            textBoxOF.Text = String.Format("{0:X}", CCR[2]);
            textBoxUF.Text = String.Format("{0:X}", CCR[3]);
        }


        private void ButtonStep_Click(object sender, RoutedEventArgs e)
        {
            int steps = 0;
            int i = 0;

            try
            {
                steps = Convert.ToUInt16(textBoxSteps.Text);           
            }
            catch (FormatException)
            {
                textBoxSteps.Text = "";
                textBoxSteps.Focus();
                WindowError popupError = new WindowError();
                popupError.textBox1.Text = "Steps can only be a positive numeric value.";
                popupError.Show();
                i = textBoxSteps.Text.Length;
            }
            catch (OverflowException)
            {
                textBoxSteps.Text = "";
                textBoxSteps.Focus();
                WindowError popupError = new WindowError();
                popupError.textBox1.Text = "Steps value must be a positive number between 1 and ";
                popupError.textBox1.Text += int.MaxValue.ToString();
                popupError.Show();
                i = textBoxSteps.Text.Length;
            }
            catch (Exception ex)
            {
                textBoxSteps.Text = "";
                textBoxSteps.Focus();
                WindowError popupError = new WindowError();
                popupError.textBox1.Text = ex.Message;
                popupError.Show();
                i = textBoxSteps.Text.Length;
            }          

            for (i = 0; i < steps; ++i)
            {
                fetch();
                execute();

            }
            updateRegisters();
            if (M1_Memory.IsVisible)
            {
                _updateMem();
            }

        }

        private void _updateMem()
        {
           string buffer = "";
           for (int i = 0; i < 256; ++i)  //brute force 
           {
               buffer += String.Format("{0:X4}", memory[i]) + " ";
           }
           M1_Memory.textBox1.Text = buffer;

        }

        private void ButtonMem_Click(object sender, RoutedEventArgs e)
        {
            if (!M1_Memory.IsVisible)
            {
               M1_Memory = new Window2();
               _updateMem();
               M1_Memory.Show();
            }
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            while (PC < line_number )
            {
                fetch();
                execute();
            }
            updateRegisters();

            if (M1_Memory.IsVisible)
            {
                _updateMem();
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            WindowCredits credits = new WindowCredits();
            credits.Show();
        }
    }
}
