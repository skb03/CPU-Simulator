#include <windows.h>           //Steve Lenig, Seth Bane
#include <stdio.h>            //Stephanie Carter, Sergio Rocha
#include <string.h>
#include <stdlib.h>
#define n_tokens 4

short label_line[4096];     // label table
short label_length[4096];
char * label_name[4096];
short label_index=0;
short line_number=0;

int new_string(char** string, short size)
{
  (*string) = (char *) malloc (size * sizeof (char));
  
  if( (*string) == NULL ) 
    return 0; // malloc failed, return false
  
  return 1; // success, return true
}
    /*
           OPCODE  |   DMODE |  DREG   |  SMODE  |  SREG
        00 01 02 03| 04 05 06| 07 08 09| 10 11 12| 13 14 15 
         0  0  0  0|  0  0  0|  0  0  0|  0  0  0|  0  0  0
    
       OPCODE
       MOV   0000
       NOT   0001           BRA   1000
       ADD   0010           BEQ   1001
       SUB   0011           BNE   1010
       OR    0100           BGT   1011
       AND   0101           BLT   1100
       CMP   0110           BRZ   1101
       XOR   0111

        MODE
        REG DIRECT                D0      000  
        REG INDIRECT             (D0)     001
        AUTO-INC                 (D0)+    010
        AUTO-INC DEFERRED       @(D0)+    011
        AUTO-DEC                -(D0)     100
        AUTO-DEC DEFERRED      @-(D0)     101

        LITERAL                   ##      111 ; LIT - > SMODE + SREG bits
 
 previously
        LEAD INDEX ADDR    ADDR X(D0)     110
        INDEX DEFERRED         @X(D0)     111
    
       REGISTER
       D0-7    000-111
      
    */

print_token( char * token, int index)
{
   int j;
   for (j=0; j< index; ++j)
      printf("%c", token[j]);

   printf(" ");
}
//______________________________________________________TYPE2__________________________________________________//
short opcode_to_logic(char * token, short * OPCODE)
{
   switch (token[0])
   {
   case 'M':
      if ( token[1] == 'O' && token[2] == 'V')
      {
         *OPCODE = 0;                 //0 -> OPCODE;   MOV             
      }
      break;
   case 'N':
      if ( token[1] == 'O' && token[2] == 'T')              
      {
         *OPCODE = 1;                 //1 -> OPCODE;   NOT              
      }
      break;
   case 'A':
      if( token[1] == 'D' && token[2] == 'D')
      {
         *OPCODE = 2;                 //2 -> OPCODE;   ADD
      }
      else if ( token[1] == 'N' && token[2] == 'D')
      {
         *OPCODE = 5;                 //5 -> OPCODE;   AND         
      }
      break;
   case 'S':
      if ( token[1] == 'U' && token[2] == 'B')
      {
         *OPCODE = 3;                   //3 -> OPCODE;   SUB
      }
      else if ( token[1] == 'H' && token[2] == 'L')
      {
         *OPCODE = 14;                  //14 -> OPCODE;   SHR
      }
      else if ( token[1] == 'H' && token[2] == 'R')
      {
         *OPCODE = 15;                  //15 -> OPCODE;   SHL
      }
      break;
   case 'O':
      if ( token[1] == 'R')
      {
         *OPCODE = 4;                   //4 -> OPCODE;   OR
      }                
      break;
   case 'C':
      if ( token[1] == 'M' && token[2] == 'P')
      {
         *OPCODE = 6;                   //6 -> OPCODE;   CMP
      }                
      break;
   case 'X':
      if ( token[1] == 'O' && token[2] == 'R')
      {
         *OPCODE = 7;                   //7 -> OPCODE;   XOR
      }                
      break;
   case 'B':
      if ( token[1] == 'R' && token[2] == 'A')
      {
         *OPCODE = 8;                   //8 -> OPCODE;   BRA
      }    
      else if ( token[1] == 'E' && token[2] == 'Q')
      {
         *OPCODE = 9;                   //9 -> OPCODE;   BEQ
      }  
      else if ( token[1] == 'N' && token[2] == 'E')
      {
         *OPCODE = 10;                 //10 -> OPCODE;   BNE
      }  
      else if ( token[1] == 'G' && token[2] == 'T')
      {
         *OPCODE = 11;                 //11 -> OPCODE;   BGT
      }  
      else if ( token[1] == 'L' && token[2] == 'T')
      {
         *OPCODE = 12;                 //12 -> OPCODE;   BLT
      }  
      else if ( token[1] == 'R' && token[2] == 'Z')
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


short mode_to_logic(char * token, short * MODE, short length)
{
   switch (token[0])    //not picky, but pickyness handled at reg_to_logic
   {
   case 'D':
      *MODE = 0;                              //0 -> MODE;     REG DIRECT
      break;
   case '(':
      if (token[length-1] == '+')
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

short reg_to_logic(char * token, short * REG)
{
   short i=0;
   if (token[i] == '@')  //picky style mode + reg detection
      ++i;
   if (token[i] == '-')  //but does not detect bad semantics after reg
      ++i;
   if (token[i] == '(')
      ++i;

   if (token[i] == 'D')      //Convert ascii char to bin
   {                         //0-7 -> REG
      *REG = token[i+1] - '0';
   }
     else
        return 1;

   return 0;
}

short lit_to_logic(char * token, short * SMODE, short * SREG, short n_digits)  //> 63 = return 1
{
   short lit_val=0;    //split literal value b/w SMODE, SREG
   short i=0;
   
   for (i=0; i< n_digits; ++i)    //convert chars to decimal value
   {
      if (token[i] < '0' || token[i] > '9')
         return 1;

      lit_val *= 10;
      lit_val += token[i] - '0';
   }

   if (lit_val > 63)
      return 1;

   *SMODE = lit_val >> 3;
   *SREG =  lit_val  & 7;        //(0 - 63) -> SMODE + SREG

   return 0;
}


short label_to_logic(char * token, short * DMODE, short * DREG, short * SMODE, short * SREG, short in_length)  //> 63 = return 1
{
   short lit_val=0;    //split literal value b/w DMODE, DREG, SMODE, SREG

   short i=0;
   short j=0;

   for (i=0; i< label_index; ++i)
   {
      if (in_length == label_length[i])
      {
         for (j=0; j< in_length; ++j)       //search for label in table, not quite rite, not in_length
         {
            if (token[j] != label_name[i][j])
               j=in_length+1;
         
         }
         if (j == in_length)
         {
            lit_val = label_line[i];
            i = label_index + 1;   
         }
      }
   }
   
   if (i == label_index)   //label not found
      return 1;
   
   if (lit_val > 4095)    // overflow
      return 1;
   
   *DMODE =  lit_val >> 9;
   *DREG  = (lit_val >> 6) & 7;
   *SMODE = (lit_val >> 3) & 7;
   *SREG  =  lit_val  & 7;        //(0 - 63) -> SMODE + SREG

   return 0;
}


   
   //Type 2 
short text_to_logic(char * in_text, int in_length, short * OPCODE, short * DMODE, short * DREG, short * SMODE, short * SREG)
{
   char* tokenized[n_tokens];

   short token_index [n_tokens];
   short index = 0;

   short opcode_mode = 1;   //set until found opcode
   short dest_mode   = 0;   //here, 1 is true
   short source_mode = 0;
   short label_mode  = 0;
   short cur_token   = 0;
   short nDelimiters = 0;
   short i=0;
   short j=0;

   for (i=0; i<n_tokens; ++i)  //create strings
   {
      new_string(&tokenized[i], in_length);   
      token_index[i] = 0;
   }

   while (in_text[index] == ' ' || in_text[index] == ',') //skip initial delimters
   {
      ++index;
   }

   for (index; index < in_length; ++index)  //tokanize string, strip delimiters
   {
      if (in_text[index] == ' ' || in_text[index] == ',')
      {
         ++nDelimiters;
         while (in_text[index] == ' ' || in_text[index] == ',') //skip subsequent consecutive delimters
         {   
            ++index;
         }
      }
      if (nDelimiters < n_tokens )  
      {
         tokenized[nDelimiters][token_index[nDelimiters]] = in_text[index];
         ++token_index[nDelimiters];
      }
   }
   
   if (tokenized[0][token_index[0]-1] == ':')  //detect label, skip
   {
      print_token( tokenized[cur_token], token_index[cur_token] ); //print out token (remove later)
      ++cur_token;
   }

   
   for (cur_token; cur_token < n_tokens; ++cur_token)         //detect everything, set appropriate vars
   { 
      if( opcode_mode)                                      //detect opcode
	  {
        print_token( tokenized[cur_token], token_index[cur_token] ); //print out token (remove later)
        
        if ( opcode_to_logic(tokenized[cur_token], OPCODE))    //if bad OPCODE, return 1
           return 1;    

	     opcode_mode = 0;

	     if (*OPCODE >= 8 && *OPCODE < 14 )
            label_mode = 1;
        else                     //opcode 8-13, ignore rest of line 
            dest_mode = 1;
            
	  }
	  else if( dest_mode)
	  {
             //detect DMODE + DREG, set out_bin
       print_token( tokenized[cur_token], token_index[cur_token] ); //print out token (remove later)
        
       if ( mode_to_logic(tokenized[cur_token], DMODE, token_index[cur_token] ))    //if bad DMODE, return 2
           return 2;
        
       if ( reg_to_logic(tokenized[cur_token], DREG ))                           //if bad DREG, return 3
           return 3;

       dest_mode   = 0;
       if ( *OPCODE != 1)   //NOT only has 1 operand
           source_mode = 1;
           
          
	  }
     else if ( source_mode)                           //detect SMODE + SREG or LIT, set out_bin
     {
        print_token( tokenized[cur_token], token_index[cur_token] ); //print out token (remove later)
        if (tokenized[cur_token][0] >= '0' && tokenized[cur_token][0] <= '9')  //detect literal
        {
           if (token_index[cur_token] > 2 ||
              lit_to_logic( tokenized[cur_token], SMODE, SREG, token_index[cur_token] ))  //if bad literal, return 6;
              return 6;

           *DMODE = 7;                          //set literal mode (hijacks DMODE, so only use REG DIRECT w/ it)
        }
        else
        {
           if ( mode_to_logic(tokenized[cur_token], SMODE, token_index[cur_token] ))    //if bad SMODE, return 4
              return 4;
        
           if ( reg_to_logic(tokenized[cur_token], SREG))                         //if bad SREG, return 5
              return 5;
        }
        source_mode = 0; 
     }
     else if ( label_mode)
     {
        print_token( tokenized[cur_token], token_index[cur_token] ); //print out token (remove later)
          
        if ( label_to_logic( tokenized[cur_token], DMODE, DREG, SMODE, SREG, token_index[cur_token]))
           return 7;                                                       //if bad label, return 7
        
        label_mode = 0;
     }
  }

   for (i=0; i<n_tokens; ++i)
      free(tokenized[i]);

   return 0;
}
//____________________________________________________END_TYPE2_____________________________________________//



//____________________________________________________MAIN_________________________________________________//

int main (int argc, char*argv[])   // goal is to detect OPCODE, MODES, REGISTERS, and LABELS from single       
{                                 // string of input, translate and output  
              
   char c = 'f';
   char * in_text;
   char* out_bin;

   short OPCODE, DMODE, DREG, SMODE, SREG;
   short * OPC_PTR = &OPCODE;
   short * DMO_PTR = &DMODE;
   short * DRE_PTR = &DREG;
   short * SMO_PTR = &SMODE;
   short * SRE_PTR = &SREG;
   short i  = 0;
   short j  = 0;
   short er = 0;
   short n_lines = 6;
   short in_length = 37;    //could be any length line, made it singular for easier testing

   char * error_msg[7];
   char * text[6];        //set to text[n_lines] (not really necessary if just looping through file)


   for (i=0; i<7; ++i)   //init blocks of info
   {
      new_string(&error_msg[i], 30);
   }

   for (i=0; i<n_lines; ++i)
   {
      new_string(&text[i], 37);
   }

   error_msg[0] = "Invalid opcode                ";   //block of error messages
   error_msg[1] = "Invalid destination mode      ";
   error_msg[2] = "Invalid destination register  ";
   error_msg[3] = "Invalid source mode           ";
   error_msg[4] = "Invalid source register       ";
   error_msg[5] = "Invalid source literal        ";
   error_msg[6] = "Invalid lable                 ";

   text[0] = ",  ,, ,  ,   ,MOV,  ,, ,D6, ,, ,D7 ,,";   //block of testing lines
   text[1] = ", ,LABEL:, ,NOT, , ,(D5), , ,, , ,  ,";
   text[2] = " ,LABEL:, ,ADD, , ,@(D3), , ,@(D4)+ ,";   //examle of line w/ error
   text[3] = " ,LABEL:, ,XOR, , ,-(D3), , ,@-(D4) ,";
   text[4] = ",   ,LABEL:,  ,BRZ, ,  ,OTR_LBL, ,  ,";
   text[5] = ",   ,LABEL:,  ,MOV, ,  ,D3, , ,63   ,";   


   new_string(&in_text, in_length);  //not really using in_text, just an example
   new_string(&out_bin, 16);

   printf("\n");
   for (i=0; i<16; ++i)           //init bin string
      out_bin[i]='0';

   
   FILE* inFile;
   FILE* outFile;
   FILE* errorFile;
   
   char inFileName[260];
   char * in_file_name;
   char outFileName[26];
   short fn_index = 0;
  
   printf("\n");
   inFile = fopen(argv[1], "r");
   
   short instruction = 0;
   
   while(fgets(in_text, 37, inFile))  //pre-complition label resolution
   {
      for(i=0; i<37; ++i)
      {
         if (in_text[i] == ':')
         {
            label_length[label_index] = i;
            label_line[label_index] = line_number;
            new_string(&label_name[label_index],i);
            for (j=0; j<i; ++j)
            {
               label_name[label_index][j] = in_text[j];   
            }
            ++label_index;
            i = 37;   
         }
      }
      
      ++line_number;
   
   }

   fclose( inFile);
   
   inFile = fopen(argv[1],"r");
   outFile = fopen("m1out.txt", "w");
   errorFile = fopen("m1errors.txt", "w");
   line_number =0;
   
   while(fgets(in_text, 37, inFile))
   {
      
	   er = text_to_logic(in_text, 35, OPC_PTR, DMO_PTR, DRE_PTR, SMO_PTR, SRE_PTR);
	   instruction += OPCODE << 12;
	   instruction += DMODE  << 9;
	   instruction += DREG   << 6;
	   instruction += SMODE  << 3;
	   instruction += SREG;
	   printf(", %X ", instruction );
	   fprintf(outFile,"%X ", instruction );
	   if (er > 1)
	   {
        printf(" ! ERROR: "); 
        printf(error_msg[er-1]); 
        
        fprintf(errorFile, "Line %i: ",line_number);
        fprintf(errorFile, error_msg[er-1]);
        fprintf(errorFile, "\n");
      }
	   
	   printf("\n");
	   
	   fprintf(outFile, "\n");
	   instruction = 0;

	   ++line_number;  
   }
   
   fclose( outFile);
   outFile = fopen("m1symtbl.txt", "w");
   
   if (label_index > 0)
   {
      fprintf(outFile, "SYMBOL TABLE INFORMATION\n");
      fprintf(outFile, "Symbol name          Line\n");
      fprintf(outFile, "-------------------------\n");
   }
   for (i=0; i< label_index; ++i)
   {
       for (j=0; j< label_length[i]; ++j)
       {
           fprintf(outFile, "%c", label_name[i][j]);
       }
       for (j; j< 23; ++j)
       {
           fprintf(outFile, " ");
       }
       fprintf(outFile, "%i\n", label_line[i]);
   }
   
   
   fclose( inFile);
   fclose( errorFile);
   fclose( outFile);
   free (in_text);
   free (out_bin);

  // scanf("c",c);        //pause at end to view stuff (if not run from command prmpt)
                          // relplace w/ "scanf("c",c")" if not compiling w/ MS VS 

   return 0;
}


