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
    /// Interaction logic for Window1.xaml
    /// </summary>
    unsafe public partial class Window1 : Window
    {
        short []label_line;     // label table
        short []label_length;
        char * []label_name;
        short label_index=0;
        short line_number=0;

        public Window1()
        {
            InitializeComponent();
        }

        short opcode_to_logic(char* token, short* OPCODE)
        {
            switch (token[0])
            {
                case 'M':
                    if (token[1] == 'O' && token[2] == 'V')
                    {
                        *OPCODE = 0;                 //0 -> OPCODE;   MOV             
                    }
                    break;
                case 'N':
                    if (token[1] == 'O' && token[2] == 'T')
                    {
                        *OPCODE = 1;                 //1 -> OPCODE;   NOT              
                    }
                    break;
                case 'A':
                    if (token[1] == 'D' && token[2] == 'D')
                    {
                        *OPCODE = 2;                 //2 -> OPCODE;   ADD
                    }
                    else if (token[1] == 'N' && token[2] == 'D')
                    {
                        *OPCODE = 5;                 //5 -> OPCODE;   AND         
                    }
                    break;
                case 'S':
                    if (token[1] == 'U' && token[2] == 'B')
                    {
                        *OPCODE = 3;                   //3 -> OPCODE;   SUB
                    }
                    break;
                case 'O':
                    if (token[1] == 'R')
                    {
                        *OPCODE = 4;                   //4 -> OPCODE;   OR
                    }
                    break;
                case 'C':
                    if (token[1] == 'M' && token[2] == 'P')
                    {
                        *OPCODE = 6;                   //6 -> OPCODE;   CMP
                    }
                    break;
                case 'X':
                    if (token[1] == 'O' && token[2] == 'R')
                    {
                        *OPCODE = 7;                   //7 -> OPCODE;   XOR
                    }
                    break;
                case 'B':
                    if (token[1] == 'R' && token[2] == 'A')
                    {
                        *OPCODE = 8;                   //8 -> OPCODE;   BRA
                    }
                    else if (token[1] == 'E' && token[2] == 'Q')
                    {
                        *OPCODE = 9;                   //9 -> OPCODE;   BEQ
                    }
                    else if (token[1] == 'N' && token[2] == 'E')
                    {
                        *OPCODE = 10;                 //10 -> OPCODE;   BNE
                    }
                    else if (token[1] == 'G' && token[2] == 'T')
                    {
                        *OPCODE = 11;                 //11 -> OPCODE;   BGT
                    }
                    else if (token[1] == 'L' && token[2] == 'T')
                    {
                        *OPCODE = 12;                 //12 -> OPCODE;   BLT
                    }
                    else if (token[1] == 'R' && token[2] == 'Z')
                    {
                        *OPCODE = 13;                 //13 -> OPCODE;   BRZ
                    }
                    break;
                default:
                    return 1;         //not an opcode, return 1
                    break;
            }

            return 0;
        }


        short mode_to_logic(char* token, short* MODE, short length)
        {
            switch (token[0])    //not picky, but pickyness handled at reg_to_logic
            {
                case 'D':
                    *MODE = 0;                              //0 -> MODE;     REG DIRECT
                    break;
                case '(':
                    if (token[length - 1] == '+')
                    {
                        *MODE = 2;                           //2 -> MODE;    AUTO-INC
                    }
                    else
                    {
                        if (token[3] == ')')
                        {
                            *MODE = 1;                        //1 -> MODE;    REG INDIRECT
                        }
                    }
                    break;
                case '@':
                    if (token[1] == '-')
                    {
                        *MODE = 5;                           //5 -> MODE;    AUTO-DEC DEFERRED
                    }
                    else
                    {
                        if (token[length - 1] == '+')
                        {
                            *MODE = 3;                       //3 -> MODE;    AUTO-INC DEFERRED     
                        }
                        else
                            return 1;
                    }
                    break;
                case '-':
                    if (length < 6 && token[1] == '(' && token[4] == ')')
                    {
                        *MODE = 4;                          //4 -> MODE;    AUTO-DEC
                    }
                    break;
                default:
                    return 1;
                    break;

            }
            return 0;

        }

        short reg_to_logic(char* token, short* REG)
        {
            short i = 0;
            if (token[i] == '@')  //picky style mode + reg detection
                ++i;
            if (token[i] == '-')  //but does not detect bad semantics after reg
                ++i;
            if (token[i] == '(')
                ++i;

            if (token[i] == 'D')      //Convert ascii char to bin
            {                         //0-7 -> REG
                *REG = (short)(token[i + 1] - '0');
            }
            else
                return 1;

            return 0;
        }


        short lit_to_logic(char* token, short* SMODE, short* SREG, short n_digits)  //> 63 = return 1
        {
            short lit_val = 0;    //split literal value b/w SMODE, SREG
            short i = 0;

            for (i = 0; i < n_digits; ++i)    //convert chars to decimal value
            {
                if (token[i] < '0' || token[i] > '9')
                    return 1;

                lit_val *= 10;
                lit_val += (short)(token[i] - '0');
            }

            if (lit_val > 63)
                return 1;

            *SMODE = (short)(lit_val >> 3);
            *SREG = (short)(lit_val & 7);        //(0 - 63) -> SMODE + SREG

            return 0;
        }


        short label_to_logic(char* token, short* DMODE, short* DREG, short* SMODE, short* SREG, short in_length)  //> 63 = return 1
        {
            short lit_val = 0;    //split literal value b/w DMODE, DREG, SMODE, SREG

            short i = 0;
            short j = 0;

            for (i = 0; i < label_index; ++i)
            {
                if (in_length == label_length[i])
                {
                    for (j = 0; j < in_length; ++j)       //search for label in table, not quite rite, not in_length
                    {
                        if (token[j] != label_name[i][j])
                            j = (short)(in_length + 1);

                    }
                    if (j == in_length)
                    {
                        lit_val = label_line[i];
                        i = (short)(label_index + 1);
                    }
                }
            }

            if (i == label_index)   //label not found
                return 1;

            if (lit_val > 4095)    // overflow
                return 1;

            *DMODE = (short)(lit_val >> 9);
            *DREG = (short)((lit_val >> 6) & 7);
            *SMODE = (short)(lit_val >> 3) & 7);
            *SREG = (short)(lit_val & 7);        //(0 - 63) -> SMODE + SREG

            return 0;
        }




        private void Open_Click(object sender, RoutedEventArgs e)
        {
            string inFileName;
            string buffer;
            System.IO.StreamReader reader;
            System.IO.StreamWriter writer;
            Microsoft.Win32.OpenFileDialog openf = new Microsoft.Win32.OpenFileDialog();
            openf.FileName = "M1ASM.txt";
            openf.DefaultExt = ".txt";
            openf.Filter = "Text documents (.txt)|*.txt";
            openf.InitialDirectory = ".";

            Nullable<bool> is_open = openf.ShowDialog();

            if (is_open == true)
            {
                inFileName = openf.FileName;
                reader = new System.IO.StreamReader(inFileName);
                writer = new System.IO.StreamWriter("m1out.txt");

                char c = ' ';

                buffer = reader.ReadLine();

                textBoxMain.Text = buffer.ToString();





                //compile

                //update main text



            }
            else
            {
                textBoxMain.Text = "ERROR: File not found";
            }


        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Mem_Click(object sender, RoutedEventArgs e)
        {
            Window2 M1_Memory = new Window2();
            M1_Memory.Show();
        }



    }
}
