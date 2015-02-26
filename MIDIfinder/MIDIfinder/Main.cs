using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MIDIfinder
{
    public partial class Main : Form
    {
        struct ConstPoint
        {
            public int note;
            public int time;
        }

        struct FHash
        {
            public int note;
            public int deltatime;
            public string artistID;
            public string trackID;
        }

        FHash[] fpMap = new FHash[100000];
        FHash[] compareDB = new FHash[100000];

        public Main()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) // Test result.
            {
                textBox2.Text = ("Processing... 0%");
                string filename = openFileDialog1.FileName;
                Array.Clear(fpMap, 0, 100000);
                Array.Clear(compareDB, 0, 100000);
                ReadFileIntoArray(filename, fpMap);
                button2.Visible = true;
                button3.Visible = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            CompareWithDB(fpMap, compareDB);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            WriteToDB(fpMap, compareDB);
        }

        private string ShowDialog(string text, string caption)
        {
            Form prompt = new Form();
            prompt.Width = 250;
            prompt.Height = 150;
            prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
            prompt.Text = caption;
            prompt.StartPosition = FormStartPosition.CenterScreen;
            Label textLabel = new Label() { Left = 25, Top = 20, Width = 170, Text = text };
            TextBox textBox = new TextBox() { Left = 25, Top = 45, Width = 190 };
            Button confirmation = new Button() { Text = "OK", Left = 116, Width = 100, Top = 70 };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;
            prompt.ShowDialog();
            return textBox.Text;
        }

        private void ReadFileIntoArray(string filename, FHash[] fpMap)
        {
            try
            {
                ConstPoint[] constMap = new ConstPoint[100000];
                byte[] midiFile = File.ReadAllBytes(filename);
                int[] midiData = new int[midiFile.Length];
                ConvertBytesToIntegers(midiFile, midiData);
                textBox2.Text = ("Processing... 50%");
                MakeConstellationMap(midiData, constMap);
                textBox2.Text = ("Processing... 75%");
                MakeFingerprints(constMap, fpMap);
                textBox2.Text = ("Processing done");
            }

            catch (FileNotFoundException)
            {
                textBox2.Text = ("Please enter the name of a Midi file that exists\nFile:");
                string nfilename = Console.ReadLine();
                ReadFileIntoArray(nfilename, fpMap);
            }

        }
        private void ConvertBytesToIntegers(byte[] midiFile, int[] midiData)
        {
            for (int i = 0; i < midiFile.Length; i++)
            {
                midiData[i] = Convert.ToInt32(midiFile[i]);
            }
        }

        private void MakeConstellationMap(int[] midiData, ConstPoint[] constMap)
        {
            int i, u = 0;
            int offsetTime = 0;
            i = findMtrk(midiData) + 8;
            while (i < midiData.Length)
            {
                offsetTime += CalcVLV(midiData, i);
                i += FindVLVlen(midiData, i);
                if (isNoteOn(midiData, i))
                {
                    constMap[u].note = midiData[i + 1];
                    constMap[u].time = offsetTime;
                    u++;
                    i += 3;
                }
                else if (isNoteOff(midiData, i))
                    i += 3;
                else if (isMidiEvent2Parameters(midiData, i))
                    i += 3;
                else if (isMidiEvent1Parameter(midiData, i))
                    i += 2;


            }

        }

        private int findMtrk(int[] midiArray)
        {
            int i;
            for (i = 0; i < midiArray.Length; i++)
            {
                if ((midiArray[i] == 77 && midiArray[i + 1] == 84 &&
                    midiArray[i + 2] == 114 && midiArray[i + 3] == 107))
                    break;
            }
            return i;
        }

        private int FindTrackEnd(int[] midiArray)
        {
            int end;
            end = findMtrk(midiArray)
                + midiArray[findMtrk(midiArray) + 6] * 16 * 16
                + midiArray[findMtrk(midiArray) + 7];
            return end;
        }

        private int CalcVLV(int[] midiArray, int i)
        {
            int z = FindVLVlen(midiArray, i);
            int deltatime = 0;

            switch (z)
            {
                case (1):
                    deltatime = midiArray[i];
                    break;
                case (2):
                    deltatime = ((midiArray[i] - 128) * 128) + midiArray[i + 1];
                    break;
                case (3):
                    deltatime = ((midiArray[i] - 128) * 128 * 128) + ((midiArray[i + 1] - 128) * 128) + midiArray[i + 2];
                    break;
                case (4):
                    deltatime = ((midiArray[i] - 128) * 128 * 128 * 128) + ((midiArray[i + 1] - 128) * 128 * 128) + ((midiArray[i + 2] - 128) * 128) + midiArray[i + 3];
                    break;
                default:
                    deltatime = midiArray[i];
                    break;
            }
            return deltatime;
        }


        private int FindVLVlen(int[] midiArray, int i)
        {
            int size_of_deltatime = 1;
            if (midiArray[i] >= 128)
            {
                size_of_deltatime += 1;
                if (midiArray[i + 1] >= 128)
                {
                    size_of_deltatime += 1;
                    if (midiArray[i + 2] >= 128)
                    {
                        size_of_deltatime += 1;
                        if (midiArray[i + 3] >= 128)
                        {
                            size_of_deltatime += 1;
                        }
                    }
                }
            }
            return size_of_deltatime;

        }

        private bool isFF(int[] midiArray, int i)
        {
            return (midiArray[i] == 255);
        }
        private bool isNoteOn(int[] midiArray, int i)
        {
            return (midiArray[i] == 144 && midiArray[i + 1] != 0 && midiArray[i + 2] != 0)
                || (midiArray[i] == 145 && midiArray[i + 1] != 0 && midiArray[i + 2] != 0)
                || (midiArray[i] == 146 && midiArray[i + 1] != 0 && midiArray[i + 2] != 0);
        }

        private bool isNoteOff(int[] midiArray, int i)
        {
            return (midiArray[i] == 144 && midiArray[i + 2] == 0)
                || (midiArray[i] == 145 && midiArray[i + 2] == 0)
                || (midiArray[i] == 146 && midiArray[i + 2] == 0);
        }

        private bool isMidiEvent2Parameters(int[] midiArray, int i)
        {
            return ((midiArray[i] < 144 || midiArray[i] > 145) &&
                (midiArray[i] < 192 || midiArray[i] > 223) &&
                (midiArray[i] > 127 || midiArray[i] < 240));
        }

        private bool isMidiEvent1Parameter(int[] midiArray, int i)
        {
            return (midiArray[i] >= 192 || midiArray[i] <= 223);
        }


        private void MakeFingerprints(ConstPoint[] constMap, FHash[] fpMap)
        {
            for (int i = 0; constMap[i + 1].note != 0; i++)
            {
                fpMap[i].note = constMap[i].note;
                fpMap[i].deltatime = constMap[i + 1].time - constMap[i].time;
            }
        }

        private void WriteToDB(FHash[] fpMap, FHash[] compDB)
        {
            string inArtist = ShowDialog("Enter the artist of the song", "Info needed");
            string inSong = ShowDialog("Enter the track name of the song", "Info needed");

            if (inArtist.Length > 1 && inSong.Length >= 1)
            {
                string filename = FindNextEmptyInDB(fpMap, compDB, inArtist, inSong);
                if (filename != "")
                {
                    using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename, true))
                    {
                        for (int i = 0; fpMap[i].note != 0; i++)
                        {
                            file.WriteLine("{0} {1}", fpMap[i].note, fpMap[i].deltatime);
                        }
                        file.WriteLine(" {0} - {1}", inArtist, inSong);
                    }
                    textBox2.Text = String.Format("Succesfully added '{0} - {1}' to database as {2}", inArtist, inSong, filename);
                }


            }
        }

        private string FindNextEmptyInDB(FHash[] fpMap, FHash[] compDB, string inArtist, string inSong)
        {
            int i = 0;
            string filename = "0.sdb";

            while (File.Exists(filename))
            {
                if (DoesNotExist(i, fpMap, compDB, inArtist, inSong))
                {
                    i++;
                    filename = string.Format("{0}.sdb", i);
                }
                else
                {
                    filename = "";
                    break;
                }

            }

            return filename;
        }

        private bool DoesNotExist(int i, FHash[] fpMap, FHash[] compDB, string inArtist, string inSong)
        {
            ReadFromDB(compDB, i);
            int j = 0;
            while (compDB[j].artistID == "0")
                j++;
            if (CheckFile(compDB, j, inArtist, inSong))
            {
                textBox2.Text = String.Format("The song '{0} - {1}' already exists in database as '{2}.sdb'", inArtist, inSong, i);
                return false;
            }
            else
                return true;
        }

        private bool CheckFile(FHash[] compDB, int j, string inArtist, string inSong)
        {
            return (compDB[j].trackID == inSong && compDB[j].artistID == inArtist);
        }

        private void ReadFromDB(FHash[] compDB, int i)
        {
            string dbFilename = string.Format("{0}.sdb", i);
            if (File.Exists(dbFilename))
                SetFHash(dbFilename, compDB);
        }


        private void SetFHash(string dbFilename, FHash[] compDB)
        {
            int j = 0;
            string buffer;
            string[] bits = new string[2];

            using (TextReader r = File.OpenText(dbFilename))
            {
                while ((r.Peek() != -1))
                {
                    buffer = r.ReadLine();

                    if (!buffer.Contains('-'))
                    {
                        bits = buffer.Split(' ');
                        compDB[j].note = int.Parse(bits[0]);
                        compDB[j].deltatime = int.Parse(bits[1]);
                        compDB[j].artistID = "0";
                        compDB[j].trackID = "0";
                        j++;
                    }
                    else
                    {
                        bits = buffer.Split('-');
                        compDB[j].artistID = bits[0].Trim();
                        compDB[j].trackID = bits[1].Trim();
                    }
                }
            }
        }

        private void CompareWithDB(FHash[] inputDB, FHash[] compareDB)
        {
            string file = "0.sdb";
            int db_i = 0, input_i = 1,
                j = 0, z = 0,
                matchCount = 0;

            for (j = 0; File.Exists(file); j++)
            {
                file = string.Format("{0}.sdb", j);
                ReadFromDB(compareDB, j);
                for (db_i = 0; compareDB[db_i].note != 0; db_i++)
                {
                    if (isMatch(inputDB, compareDB, db_i, input_i))
                    {
                        matchCount += 1;
                        input_i += 1;
                    }
                    else
                    {
                        matchCount = 0;
                        input_i = 1;
                    }

                    if (matchCount == 5)
                    {
                        z = db_i;
                        while (compareDB[z].artistID == "0")
                            z++;
                        textBox2.Text = String.Format("{0} - {1}", compareDB[z].artistID, compareDB[z].trackID);
                        break;
                    }
                }

                if (matchCount == 5)
                    break;

            }
            if (matchCount < 5)
                textBox2.Text = "No match found.";
        }

        private bool isMatch(FHash[] fpMap, FHash[] compareDB, int i, int q)
        {
            int FLUFF = 150;

            return (fpMap[q].note == compareDB[i].note &&
                    fpMap[q].deltatime >= compareDB[i].deltatime - FLUFF &&
                    fpMap[q].deltatime <= compareDB[i].deltatime + FLUFF);
        }
    }
}
