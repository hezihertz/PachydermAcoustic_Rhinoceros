﻿//'Pachyderm-Acoustic: Geometrical Acoustics for Rhinoceros (GPL) by Arthur van der Harten 
//' 
//'This file is part of Pachyderm-Acoustic. 
//' 
//'Copyright (c) 2008-2015, Arthur van der Harten 
//'Pachyderm-Acoustic is free software; you can redistribute it and/or modify 
//'it under the terms of the GNU General Public License as published 
//'by the Free Software Foundation; either version 3 of the License, or 
//'(at your option) any later version. 
//'Pachyderm-Acoustic is distributed in the hope that it will be useful, public static void Write_Pac1
//'but WITHOUT ANY WARRANTY; without even the implied warranty of 
//'MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the 
//'GNU General Public License for more details. 
//' 
//'You should have received a copy of the GNU General Public 
//'License along with Pachyderm-Acoustic; if not, write to the Free Software 
//'Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA. 

using System;

namespace Pachyderm_Acoustic
{
    namespace Utilities
    {
        public class FileIO
        {
            /// <summary>
            /// Writes a Pac1 file. [Useable by scripts and interface components alike.]
            /// </summary>
            /// <param name="Direct_Data">Array of Completed Direct Sound Simulations Required</param>
            /// <param name="IS_Data">Array of Completed Image Source Simulations. Enter null if opted out.</param>
            /// <param name="Receiver">Array of Completed Ray-Tracing Simulation Receivers. Enter null if opted out.</param>
            public static void Write_Pac1(ref Direct_Sound[] Direct_Data, ref ImageSourceData[] IS_Data, ref Environment.Receiver_Bank[] Receiver)
            {
                System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
                sf.DefaultExt = ".csv";
                sf.AddExtension = true;
                sf.Filter = "Pachyderm Ray Data file (*.csv)|*.csv|" + "All Files|";

                if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Write_Pac1(sf.FileName, ref Direct_Data, ref IS_Data, ref Receiver);
                }
            }

            /// <summary>
            /// Writes a Pac1 file. [Useable by scripts and interface components alike.]
            /// </summary>
            /// <param name="filename">The location of the final saved file...</param>
            /// <param name="Direct_Data">Array of Completed Direct Sound Simulations Required</param>
            /// <param name="IS_Data">Array of Completed Image Source Simulations. Enter null if opted out.</param>
            /// <param name="Receiver">Array of Completed Ray-Tracing Simulation Receivers. Enter null if opted out.</param>
            public static void Write_Pac1(string filename, ref Direct_Sound[] Direct_Data, ref ImageSourceData[] IS_Data, ref Environment.Receiver_Bank[] Receiver)
            {
                if (Direct_Data == null && IS_Data == null && IS_Data == null && Receiver != null)
                {
                    System.Windows.Forms.MessageBox.Show("There is no simulated data to save.");
                    return;
                }
                
                Pachyderm_Acoustic.UI.PachydermAc_PlugIn plugin = Pachyderm_Acoustic.UI.PachydermAc_PlugIn.Instance;

                //use StreamWriter to write to csv files
                System.IO.StreamWriter sw = System.IO.File.CreateText(filename);

                //1. Date & Time
                sw.WriteLine("Current Date and Time");
                sw.WriteLine(System.DateTime.Now.ToString());

                //2. Plugin Version... if less than 1.1, assume only 1 source.
                sw.WriteLine("Plugin Version");
                sw.WriteLine(plugin.Version);

                //3. Cut off Time (seconds) and SampleRate
                sw.WriteLine("Cut off Time");
                sw.WriteLine((double)Receiver[0].CO_Time);//CO_TIME.Value);
                sw.WriteLine("Sample Rate");
                sw.WriteLine(Receiver[0].SampleRate);

                //4.0 Source Count(int)
                Rhino.Geometry.Point3d[] SRC;
                plugin.SourceOrigin(out SRC);
                sw.WriteLine("Source Count");
                sw.WriteLine(SRC.Length);

                //4 Source Location x,y,z (double)
                sw.WriteLine("Source Location : x y z");
                for (int i = 0; i < SRC.Length; i++)
                    sw.WriteLine(Helper_Functions.ConvertToCSVString(SRC[i].X, SRC[i].Y, SRC[i].Z));

                //5. No of Receivers
                sw.WriteLine("Number of Receivers");
                sw.WriteLine(Receiver[0].Rec_List.Length);

                //6. Write the coordinates of each receiver point
                //6b. Write the environmental characteristics at each receiver point (Rho * C); V2.0 only...
                for (int q = 0; q < Receiver[0].Rec_List.Length; q++)
                {
                    string origin_and_rhoC = Helper_Functions.ConvertToCSVString(
                        Receiver[0].Rec_List[q].H_Origin.x,
                        Receiver[0].Rec_List[q].H_Origin.y,
                        Receiver[0].Rec_List[q].H_Origin.z,
                        Receiver[0].Rec_List[q].Rho_C);
                    sw.WriteLine(origin_and_rhoC);
                }

                for (int s = 0; s < SRC.Length; s++)
                {
                    if (Direct_Data != null)
                    {
                        //7. Write Direct Sound Data
                        Direct_Data[s].Write_Data(ref sw);
                    }

                    if (IS_Data[0] != null)
                    {
                        //8. Write Image Source Sound Data
                        IS_Data[s].Write_Data(ref sw);
                    }

                    if (Receiver != null)
                    {
                        //9. Write Ray Traced Sound Data
                        Receiver[s].Write_Data(ref sw);
                    }
                }
                sw.Write("End");
                sw.Close();
            }

            /// <summary>
            /// Writes a Pac1 file. [Useable by scripts and interface components alike.]
            /// </summary>
            /// <param name="Direct_Data">Empty array of Direct Sound Simulations Required</param>
            /// <param name="IS_Data">Empty array of Image Source Simulations. Enter null if opted out.</param>
            /// <param name="Receiver">Empty array of Ray-Tracing Simulation Receivers. Enter null if opted out.</param>
            public static bool Read_Pac1(ref Direct_Sound[] Direct_Data, ref ImageSourceData[] IS_Data, ref Environment.Receiver_Bank[] Receiver)
            {
                    System.Windows.Forms.OpenFileDialog sf = new System.Windows.Forms.OpenFileDialog();
                    sf.DefaultExt = ".pac1";
                    sf.AddExtension = true;
                    sf.Filter = "Pachyderm Simulation (*.pac1)|*.pac1|" + "All Files|";
                    if (sf.ShowDialog() != System.Windows.Forms.DialogResult.OK) return false;

                    return Read_Pac1(sf.FileName, ref Direct_Data, ref IS_Data, ref Receiver);
            }


            public static bool Read_Pac1(string filename, ref Direct_Sound[] Direct_Data, ref ImageSourceData[] IS_Data, ref Environment.Receiver_Bank[] Receiver)
            {
                 System.IO.BinaryReader sr = new System.IO.BinaryReader(System.IO.File.Open(filename, System.IO.FileMode.Open));
                try
                {
                    //1. Date & Time
                    string Savedate = sr.ReadString();
                    //2. Plugin Version
                    string Pach_version = sr.ReadString();
                    //3. Cut off Time and SampleRate
                    double CO_TIME = sr.ReadDouble();
                    int SampleRate = sr.ReadInt32();
                    //4. Source Count          
                    int SrcCt = 1;
                    if (double.Parse(Pach_version.Substring(0, 3)) >= 1.1) SrcCt = sr.ReadInt32();
                    //4.1 Source Location x                   
                    //4.2 Source Location y
                    //4.3 Source Location z
                    Hare.Geometry.Point[] SrcPt = new Hare.Geometry.Point[SrcCt];
                    for (int s = 0; s < SrcCt; s++) SrcPt[s] = new Hare.Geometry.Point(sr.ReadDouble(), sr.ReadDouble(), sr.ReadDouble());
                    //5. No of Receivers
                    int Rec_Ct = sr.ReadInt32();
                    //6. Write the coordinates of each receiver point
                    //6b. Write the environmental characteristics at each receiver point (Rho * C); V2.0 only...
                    Hare.Geometry.Point[] Recs = new Hare.Geometry.Point[Rec_Ct];
                    double[] Rho_C = new double[Rec_Ct];
                    for (int q = 0; q < Rec_Ct; q++)
                    {
                        Recs[q] = new Hare.Geometry.Point(sr.ReadDouble(), sr.ReadDouble(), sr.ReadDouble());
                        if (double.Parse(Pach_version.Substring(0, 3)) >= 2.0) Rho_C[q] = sr.ReadDouble();
                        else Rho_C[q] = 400;
                    }

                    Direct_Data = new Direct_Sound[SrcCt];
                    IS_Data = new ImageSourceData[SrcCt];
                    Receiver = new Environment.Receiver_Bank[SrcCt];

                    int DDCT = 0;
                    int ISCT = 0;
                    int RTCT = 0;
                    do
                    {
                        string readin = sr.ReadString();
                        switch (readin)
                        {
                            case "Direct_Sound":
                            case "Direct_Sound w sourcedata":
                                //9. Read Direct Sound Data
                                Direct_Data[DDCT] = Direct_Sound.Read_Data(ref sr, Recs, SrcPt[DDCT], Rho_C, Pach_version);
                                Direct_Data[DDCT].CO_Time = CO_TIME;
                                Direct_Data[DDCT].SampleFreq = (int)SampleRate;
                                DDCT++;
                                break;
                            case "Image-Source_Data":
                                //10. Read Image Source Sound Data
                                IS_Data[ISCT] = ImageSourceData.Read_Data(ref sr, Recs.Length, Direct_Data[DDCT - 1], false, ISCT, Pach_version);
                                ISCT++;
                                break;
                            case "Ray-Traced_Data":
                                //11. Read Ray Traced Sound Data
                                Receiver[RTCT] = Environment.Receiver_Bank.Read_Data(ref sr, Rec_Ct, Recs, Rho_C, Direct_Data[RTCT].Delay_ms, ref SampleRate, Pach_version);
                                RTCT++;
                                break;
                            case "End":
                                sr.Close();
                                return true;
                        }
                    } while (true);
                }
                catch (System.Exception X)
                {
                    sr.Close();
                    System.Windows.Forms.MessageBox.Show("File Read Failed...", String.Format("Results file was corrupt or incomplete. We apologize for this inconvenience. Please report this to the software author. It will be much appreciated. \r\n Exception Message: {0}. \r\n Method: {1}" , X.Message, X.TargetSite));
                    return false;
                }
            }
            
            /// <summary>
            /// Writes the map receiver to a file. 
            /// </summary>
            /// <param name="Rec_List">The list of receivers to be written.</param>
            public static void Write_pachm(Mapping.PachMapReceiver[] Rec_List)
            {
                System.Windows.Forms.SaveFileDialog sf = new System.Windows.Forms.SaveFileDialog();
                sf.DefaultExt = ".csv";
                sf.AddExtension = true;
                sf.Filter = "Pachyderm Mapping Data File (*.csv)|*.csv|" + "All Files|";
                if (sf.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    Write_pachm(sf.FileName, ref Rec_List);
                }
            }

            /// <summary>
            /// Writes pachyderm mapping file.
            /// </summary>
            /// <param name="filename">The location the new file is to be written to.</param>
            /// <param name="Rec_List">The list of receivers to be written.</param>
            public static void Write_pachm(string filename, ref Mapping.PachMapReceiver[] Rec_List)
            {
                // use Streamwriter to write to csv files 
                System.IO.StreamWriter sw = System.IO.File.CreateText(filename);
                //1. Write calculation type. (string)  
                sw.WriteLine("Calculation Tyle:");
                sw.WriteLine(Rec_List[0].Data_Type());
                Boolean Directional = Rec_List[0].Data_Type() == "Type;Map_Data";
                //2. Write the number of samples in each histogram. (int)
                sw.WriteLine("Number of samples in each histogram:"); 
                sw.WriteLine((UInt32)Rec_List[0].SampleCT);
                //3. Write the sample rate. (int)
                sw.WriteLine("Sample Rate:");
                sw.WriteLine((UInt32)Rec_List[0].SampleRate);
                //4. Write the number of Receivers (int)
                int Rec_Ct = Rec_List[0].Rec_List.Length;
                sw.WriteLine("Number of Receivers:");
                sw.WriteLine((UInt32)Rec_Ct);
                //4.5 Announce the Version
                sw.WriteLine("Version");
                sw.WriteLine(UI.PachydermAc_PlugIn.Instance.Version);
                //5. Announce that the following data pertains to the form of the analysis mesh. (string)
                sw.WriteLine("Mesh Information:");
                //6. Announce Mesh Vertices (string)
                sw.WriteLine("Mesh Vertices:");
                //Write the number of vertices (int) (int)
                sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Vertices.Count);

                for (int i = 0; i < Rec_List[0].Map_Mesh.Vertices.Count; i++)
                {
                    //Write Vertex: (double) (double) (double)
                    /*sw.WriteLine(Rec_List[0].Map_Mesh.Vertices[i].X);
                    sw.WriteLine(Rec_List[0].Map_Mesh.Vertices[i].Y);
                    sw.WriteLine(Rec_List[0].Map_Mesh.Vertices[i].Z); */
                    string meshvertice = Helper_Functions.ConvertToCSVString(
                        Rec_List[0].Map_Mesh.Vertices[i].X,
                        Rec_List[0].Map_Mesh.Vertices[i].Y,
                        Rec_List[0].Map_Mesh.Vertices[i].Z);
                    sw.WriteLine(meshvertice);
                }
                //7. Announce Mesh Faces (string)
                sw.WriteLine("Mesh Faces:");
                // Write the number of faces 
                sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Faces.Count);

                for (int i = 0; i < Rec_List[0].Map_Mesh.Faces.Count; i++)
                {
                    // Write mesh vertex indices: (int) (int) (int) (int)
                    /*sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Faces[i][0]);
                    sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Faces[i][1]);
                    sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Faces[i][2]);
                    sw.WriteLine((UInt32)Rec_List[0].Map_Mesh.Faces[i][3]);*/
                    string meshfaces = Helper_Functions.ConvertToCSVString(
                        (UInt32)Rec_List[0].Map_Mesh.Faces[i][0],
                        (UInt32)Rec_List[0].Map_Mesh.Faces[i][1],
                        (UInt32)Rec_List[0].Map_Mesh.Faces[i][2],
                        (UInt32)Rec_List[0].Map_Mesh.Faces[i][3]);
                    sw.WriteLine(meshfaces);
                }
                //7.5: Announce the number of sources.
                //sw.WriteLine("Sources");
                sw.WriteLine("Number of Receivers:");
                sw.WriteLine(Rec_List.Length);
                //7.5a: Announce the Type of Source
                sw.WriteLine("Source Coordinate, Type, and delaytime in ms:");
                for (int i = 0; i < Rec_List.Length; i++)
                {
                    /*
                    sw.WriteLine(Rec_List[i].Src.X);
                    sw.WriteLine(Rec_List[i].Src.Y);
                    sw.WriteLine(Rec_List[i].Src.Z);
                    //////////////////////*/
                    sw.WriteLine(Helper_Functions.ConvertToCSVString(Rec_List[i].Src.X, Rec_List[i].Src.Y, Rec_List[i].Src.Z));
                    sw.WriteLine(Rec_List[i].SrcType);
                    sw.WriteLine(Rec_List[i].delay_ms);//v.2.0.0.1
                }

                //8. Announce that the following data pertains to the receiver histograms (string)
                sw.WriteLine("Receiver Hit Data:");
                //8a. Announce whether or not data is linked to vertices rather than faces (bool)
                sw.WriteLine(Rec_List[0].Rec_Vertex);
                // this part need to be rewritten for readability//
                for (int s = 0; s < Rec_List.Length; s++)
                {
                    for (int i = 0; i < Rec_Ct; i++)
                    {
                        //Write Receiver Index (int)
                        sw.WriteLine("Receiver Index:");
                        sw.WriteLine((UInt32)i);
                        //Write the direct sound arrival time.
                        sw.WriteLine("Direct Sound Arrival Time:");
                        sw.WriteLine((Rec_List[s].Rec_List[i] as Mapping.PachMapReceiver.Map_Receiver).Direct_Time);
                        //Write Impedance of Air
                        sw.WriteLine("Air Impedance:");
                        sw.WriteLine(Rec_List[0].Rec_List[i].Rho_C);

                        for (int Octave = 0; Octave < 8; Octave++)
                        {
                            //Write Octave (int)
                            sw.WriteLine("Octave band:");
                            sw.WriteLine((UInt32)Octave);
                            //Write each energy value in the histogram (double)...
                            sw.WriteLine("energy level of each receiver:");
                            double[] Hist = Rec_List[s].Rec_List[i].GetEnergyHistogram(Octave);
                            for (int e = 0; e < Rec_List[s].SampleCT; e++)
                            {
                                sw.WriteLine(Helper_Functions.ConvertToCSVString((UInt32)e, (Hist[e])));
                                //Write each directional value in the histogram (double) (double) (double);
                                if (Directional)
                                {
                                    Hare.Geometry.Vector DirPos = Rec_List[s].Directions_Pos(Octave, e, i);
                                    Hare.Geometry.Vector DirNeg = Rec_List[s].Directions_Neg(Octave, e, i); 
                                    /*sw.WriteLine(DirPos.x);
                                    sw.WriteLine(DirPos.y);
                                    sw.WriteLine(DirPos.z);
                                    sw.WriteLine(DirNeg.x);
                                    sw.WriteLine(DirNeg.y);
                                    sw.WriteLine(DirNeg.z);*/
                                    sw.WriteLine(Helper_Functions.ConvertToCSVString(DirPos.x, DirPos.y, DirPos.z));
                                    sw.WriteLine(Helper_Functions.ConvertToCSVString(DirNeg.x, DirNeg.y, DirNeg.z));
                                }
                            }
                        }
                        sw.WriteLine("End_Receiver_Hits");
                    }
                }
                sw.WriteLine("End_of_File");
                sw.Close();
            }

            public static bool Read_pachm(ref Mapping.PachMapReceiver[] Map)
            {
                System.Windows.Forms.OpenFileDialog of = new System.Windows.Forms.OpenFileDialog();
                of.DefaultExt = ".pachm";
                of.AddExtension = true;
                of.Filter = "Pachyderm Mapping Data File (*.pachm)|*.pachm|" + "All Files|";
                if (of.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    Map = null;
                    return false;
                }

                return Read_pachm(of.FileName, ref Map);
            }

            /// <summary>
            /// reads a file and populates the map receiver instance.
            /// </summary>
            /// <returns></returns>
            public static bool Read_pachm(string path, ref Mapping.PachMapReceiver[] Map)
            {
                System.IO.BinaryReader sr = new System.IO.BinaryReader(System.IO.File.Open(path, System.IO.FileMode.Open));
                //1. Write calculation type. (string)
                string CalcType = sr.ReadString();
                if (CalcType != "Type;Map_Data" && CalcType != "Type;Map_Data_NoDir") throw new Exception("Map Data File Expected");
                bool Directional = (CalcType == "Type;Map_Data");

                //2. Write the number of samples in each histogram. (int)
                int SampleCT = (int)sr.ReadUInt32();
                //3. Write the sample rate. (int) 
                int SampleRate = (int)sr.ReadUInt32();
                //4. Write the number of Receivers (int)
                int Rec_CT = (int)sr.ReadUInt32();
                //4.5 Write the version number
                double version = 1.1;
                double rev = 0;
                //5. Announce that the following data pertains to the form of the analysis mesh. (string)
                int s_ct=1;
                Rhino.Geometry.Mesh Map_Mesh = new Rhino.Geometry.Mesh();
                Map = new Mapping.PachMapReceiver[1];
                //Map[0] = new Pach_Map_Receiver();        
                //double[] Rho_C = null;
                double[] delay;
                 
                do
                {
                    switch (sr.ReadString())
                    {
                        case "Version":
                            //Pach1.7 = Versioning functionality added.
                            string v = sr.ReadString();
                            version = double.Parse(v.Substring(0, 3));
                            rev = int.Parse(v.Split(new char[1] { '.' })[3]);
                            break;
                        case "Mesh Information":
                            //6. Announce Mesh Vertices (string)
                            //Write the number of vertices & faces (int) (int)
                            if (sr.ReadString() != "Mesh Vertices") throw new Exception("Mesh Vertices Expected");

                            int VC = (int)sr.ReadUInt32();
                            int FC = (int)sr.ReadUInt32();
                            for (int i = 0; i < VC; i++)
                            {
                                //Write Vertex: (double) (double) (double)
                                Map_Mesh.Vertices.Add(new Rhino.Geometry.Point3d(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle()));
                            }

                            //7. Announce Mesh Faces (string)
                            if (sr.ReadString() != "Mesh Faces") throw new Exception("Mesh Faces Expected");

                            for (int i = 0; i < FC; i++)
                            {
                                // Write mesh vertex indices: (int) (int) (int) (int)
                                Map_Mesh.Faces.AddFace((int)sr.ReadUInt32(), (int)sr.ReadUInt32(), (int)sr.ReadUInt32(), (int)sr.ReadUInt32());
                            }
                            break;
                        case "Sources":
                            //7.5: Announce the number of sources.
                            s_ct = sr.ReadInt32();
                            delay = new double[s_ct];
                            Map = new Mapping.PachMapReceiver[s_ct];
                            //7.5a Announce the type of source.

                            for (int s = 0; s < s_ct; s++)
                            {
                                Map[s] = new Mapping.PachMapReceiver();
                                Map[s].CutOffTime = (double)SampleCT / (double)SampleRate;
                                Map[s].SampleCT = SampleCT;
                                Map[s].SampleRate = SampleRate;
                                Map[s].Map_Mesh = Map_Mesh;
                                Map[s].Rec_List = new Mapping.PachMapReceiver.Map_Receiver[Rec_CT];
                                Map[s].SrcType = sr.ReadString();
                                //4.4 Source delay (ms)
                                if (version > 2.0 || (version == 2.0 && rev >= 1))
                                {
                                    delay[s] = sr.ReadDouble();
                                }
                            }
                            break;
                        case "SourceswLoc":
                            //7.5: Announce the number of sources.
                            s_ct = sr.ReadInt32();
                            delay = new double[s_ct];
                            Map = new Mapping.PachMapReceiver[s_ct];
                            //7.5a Announce the type of source.

                            for (int s = 0; s < s_ct; s++)
                            {
                                Map[s] = new Mapping.PachMapReceiver();
                                Map[s].CutOffTime = (double)SampleCT / (double)SampleRate * 1000;
                                Map[s].SampleCT = SampleCT;
                                Map[s].SampleRate = SampleRate;
                                Map[s].Map_Mesh = Map_Mesh;
                                Map[s].Rec_List = new Mapping.PachMapReceiver.Map_Receiver[Rec_CT];
                                Map[s].Src = new Rhino.Geometry.Point3d(sr.ReadDouble(), sr.ReadDouble(), sr.ReadDouble());
                                Map[s].SrcType = sr.ReadString();
                                //4.4 Source delay (ms)
                                if (version > 2.0 || (version == 2.0 && rev >= 1))
                                {
                                    delay[s] = sr.ReadDouble();
                                }
                            }
                            break;
                        case "Receiver Hit Data":
                            if (Map[0] == null)
                            {
                                Map = new Mapping.PachMapReceiver[1];
                                Map[0] = new Mapping.PachMapReceiver();
                                Map[0].CutOffTime = (double)SampleCT / (double)SampleRate;
                                Map[0].SampleRate = SampleRate;
                                Map[0].SampleCT = SampleCT;
                                Map[0].Map_Mesh = Map_Mesh;
                                Map[0].Rec_List = new Mapping.PachMapReceiver.Map_Receiver[Rec_CT];
                                Map[0].SrcType = "Geodesic";
                            }

                            //8. Announce that the following data pertains to the receiver histograms (string)                        
                            //8a. Announce whether or not data is linked to vertices rather than faces (bool)
                            bool vert_Receiver = sr.ReadBoolean();
                            for (int s = 0; s < s_ct; s++)
                            {
                                Map[s].Rec_Vertex = vert_Receiver;
                                for (int i = 0; i < Map[s].Rec_List.Length; i++)
                                {
                                    //for version 1.7 and up, write direct sound arrival time.
                                    //Write Receiver Index (int)
                                    int j = (int)sr.ReadUInt32();
                                    //Write Direct Sound Arrival Time.
                                    double Direct_Time;
                                    if (version >= 1.7) Direct_Time = sr.ReadDouble(); else Direct_Time = (Utilities.PachTools.RPttoHPt(Map[s].Src) - Map[s].Rec_List[i].H_Origin).Length() / 343f;
                                    //Write Impedance of Air
                                    double Rho_C = version >= 2.0 ? sr.ReadDouble() : 400;

                                    if (vert_Receiver)
                                    {
                                        Map[s].Rec_List[i] = new Mapping.PachMapReceiver.Map_Receiver(Map_Mesh.Vertices[i], new Rhino.Geometry.Point3f((float)Map[s].Src.X, (float)Map[s].Src.Y, (float)Map[s].Src.Z), Direct_Time, Rho_C, i, SampleRate, SampleCT, Directional);
                                    }
                                    else 
                                    {
                                        Rhino.Geometry.Point3d RecLoc = Map_Mesh.Faces.GetFaceCenter(i);
                                        Map[s].Rec_List[i] = new Mapping.PachMapReceiver.Map_Receiver(new Rhino.Geometry.Point3f((float)RecLoc.X, (float)RecLoc.Y, (float)RecLoc.Z), new Rhino.Geometry.Point3f((float)Map[s].Src.X, (float)Map[s].Src.Y, (float)Map[s].Src.Z), Direct_Time, Rho_C, i, SampleRate, SampleCT, Directional);
                                    }

                                    for (int Octave = 0; Octave < 8; Octave++)
                                    {
                                        //Write Octave (int)
                                        int Oct_out = (int)sr.ReadUInt32();
                                        if (Oct_out != Octave) throw new Exception(string.Format("Octave {0} Expected", Octave));
                                        double[] Hist = Map[s].Rec_List[i].GetEnergyHistogram(Octave);
                                        if (Directional)
                                        {
                                            if (version < 1.7)
                                            {
                                                for (int e = 0; e < SampleCT; e++) 
                                                    Map[s].Rec_List[i].Combine_Sample(e, sr.ReadDouble(), new Hare.Geometry.Vector(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle()), new Hare.Geometry.Vector(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle()), Octave);
                                            }
                                            else
                                            {
                                                for (int e = 0; e < SampleCT; e++)
                                                    Map[s].Rec_List[i].Combine_Sample(e, sr.ReadDouble(), new Hare.Geometry.Vector(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle()), new Hare.Geometry.Vector(sr.ReadSingle(), sr.ReadSingle(), sr.ReadSingle()), Octave);
                                            }
                                        }
                                        else
                                        {
                                            if (version < 1.7)
                                            {
                                                for (int e = 0; e < SampleCT; e++)
                                                    Map[s].Rec_List[i].Combine_Sample(e, sr.ReadDouble(), new Hare.Geometry.Vector(0, 0, 0), new Hare.Geometry.Vector(0, 0, 0), Octave);
                                            }
                                            else
                                            {
                                                for (int e = 0; e < SampleCT; e++)
                                                    Map[s].Rec_List[i].Combine_Sample(e, sr.ReadDouble(), new Hare.Geometry.Vector(0, 0, 0), new Hare.Geometry.Vector(0,0,0), Octave);
                                            }                                            
                                        }
                                    }
                                    if (sr.ReadString() != "End_Receiver_Hits") throw new Exception("End of Receiver Hits Expected");
                                }
                            }
                            break;
                        case "End_of_File":
                            sr.Close();
                            return true;
                    }
                } while (true);
                throw new Exception("Unsuccessful Read");
            }
        }
    }
}