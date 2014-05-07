using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.IO;
using System.Media;
using System.Text;
using System.Windows.Forms;
using TagLib;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace MyFirstProject
{
    public partial class Form1 : Form
    {
        SoundPlayer player = null;

        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        public Form1()
        {
            InitializeComponent();

            // By the default set the volume to 0
            uint CurrVol = 0;
            // At this point, CurrVol gets assigned the volume
            waveOutGetVolume(IntPtr.Zero, out CurrVol);
            // Calculate the volume
            ushort CalcVol = (ushort)(CurrVol & 0x0000ffff);
            // Get the volume on a scale of 1 to 10 (to fit the trackbar)
            trackBar1.Value = CalcVol / (ushort.MaxValue / 10);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            PlayMIDI(Environment.CurrentDirectory + @"\nakedgun.wav");
            WriteConsoleHeaders();
        }

        private void WriteConsoleHeaders()
        {
            AppendTextBoxLine("********************************************************************************************************************************");
            AppendTextBoxLine("********************************************************* AutoTAG 9000 *********************************************************");
            AppendTextBoxLine("********************************************************************************************************************************");
        }

        private void DelimitOperations()
        {
            AppendTextBoxLine("--------------------------------------------------------------------------------------------------------------------------------");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DateTime startTime = DateTime.Now;

            if (!validatePath(textBox2.Text)) {
                AppendTextBoxLine("Source path entered is invalid, skipping operation");
                goto OpsFinished;
            }
            else if (!validatePath(textBox3.Text)) {
                AppendTextBoxLine("Destination path entered is invalid, skipping operation");
                goto OpsFinished;
            }
            else {
                AppendTextBoxLine("User input validated, starting move operation");
            }

            try
            {
                DirectoryInfo dirs = new DirectoryInfo(textBox2.Text);

                if (dirs.Exists)
                {
                    string[] mp3FilePaths = Directory.GetFiles(textBox2.Text, "*.mp3", SearchOption.AllDirectories);
                    string[] sourcePaths = Directory.GetDirectories(textBox2.Text, "*.*", SearchOption.AllDirectories);
                    string destDirectory = "";

                    // Move MP3s first
                    foreach(string mp3File in mp3FilePaths)
                    {
                        TagLib.File file = TagLib.File.Create(mp3File);

                        String filename = GetFilename(mp3File);
                        String destArtDirectory = file.Tag.FirstArtist.Replace("The ", "");
                        String destinationDirectory = textBox3.Text + @"\" + destArtDirectory + @"\" + file.Tag.Album;
                        
                        String destinationPath = cleanPath(destinationDirectory + filename);

                        if (validatePath(destinationPath))
                        {
                            CreateDirectory(destinationDirectory);
                            destDirectory = destinationDirectory;
                            MoveFile(mp3File, destinationPath);
                        }
                        else
                        {
                            AppendTextBoxLine("Artist or Album is blank, cannot move: -");
                            AppendTextBoxLine("Artist: {0} Album: {1}" + file.Tag.FirstArtist + file.Tag.Album);
                        }
                    }
                    string[] otherFilePaths = Directory.GetFiles(textBox2.Text, "*.*", SearchOption.AllDirectories);

                    // Move remaining artwork etc.
                    foreach (string otherFile in otherFilePaths)
                    {
                        String filename = GetFilename(otherFile);
                        String destinationPath = cleanPath(destDirectory + filename);

                        if (validatePath(destinationPath) && destDirectory != "")
                        {
                            MoveFile(otherFile, destinationPath);
                        }
                    }
                    destDirectory = "";

                    // Clean up empty directories
                    for (int i = (sourcePaths.Length - 1); i >= 0; i--)
                    {
                        RemoveDirectory(sourcePaths[i]);
                    }
                }
                else
                {
                    AppendTextBoxLine("Source directory does not exist");
                    goto OpsFinished;
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during MoveMP3 operation " + ex);
            }

            OpsFinished:
            TimeSpan elapsed = DateTime.Now - startTime;
            AppendTextBoxLine("Operations Finished in " + elapsed.TotalSeconds.ToString("###.###") + " seconds");
            DelimitOperations();
        }

        private void label1_Click(object sender, EventArgs e) {}

        public void AppendTextBoxLine(string myStr)
        {
            if (textBox1.Text.Length > 0)
            {
                textBox1.AppendText(Environment.NewLine);
            }
            textBox1.AppendText(myStr);
        }

        public void ClearTextBox()
        {
            textBox1.Clear();
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {}

        public void CreateDirectory(String destinationPath)
        // Checks to see if the requested directory exists, if it doesn't, it creates it
        {
            try
            {
                if (!Directory.Exists(destinationPath))
                {
                    Directory.CreateDirectory(destinationPath);
                    AppendTextBoxLine("Created " + destinationPath);
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during CreateDirectory() "+ ex);
            }
        }

        public void RemoveDirectory(String sourcePath)
        // Checks to see if the requested directory exists, if it does, it removes it
        {
            try
            {
                bool isEmptyDirectory = (Directory.GetFiles(sourcePath).Length == 0);

                if (Directory.Exists(sourcePath) && isEmptyDirectory)
                {
                    Directory.Delete(sourcePath);
                    AppendTextBoxLine("Deleted " + sourcePath);
                }
                else if (!Directory.Exists(sourcePath))
                {
                    AppendTextBoxLine("Directory doesn't exist so cannot delete " + sourcePath);
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during RemoveDirectory() " + ex);
            }
        }

        public String GetFilename(String fullPath)
        // Returns filename only e.g. 01 trackname.mp3
        {
            try
            {
                int start = fullPath.LastIndexOf("\\");
                int length = fullPath.Length - start;
                return fullPath.Substring(start, length);
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during GetFilename() "+ ex);
                return "";
            }
        }

        public String GetDirectory(String fullPath)
        // Returns directory path only e.g. c:\music\artist\album\
        {
            try
            {
                int start = 0;
                int length = fullPath.LastIndexOf("\\") - start;
                return fullPath.Substring(start, length);
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during GetDirectory() " + ex);
                return "";
            }
        }

        public void MoveFile(String sourcePath, String destinationPath)
        // Checks a file can be moved, Moves it, then checks that its been moved
        {
            try
            {
                if (System.IO.File.Exists(destinationPath))
                // Checks to see if the requested file exists, if it doesn't, it moves it to the new directory
                {
                    AppendTextBoxLine("Cannot move file - destination already exists "+ destinationPath);
                }
                else
                // Path is clear, starting move operation now
                {
                    System.IO.File.Move(sourcePath, destinationPath);
                    AppendTextBoxLine("Moved "+ sourcePath);
                    AppendTextBoxLine("   To "+ destinationPath);

                    // Final check that file is not at original location
                    if (System.IO.File.Exists(sourcePath))
                    {
                        AppendTextBoxLine("The original file still exists here somehow!! " + sourcePath);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during MoveFile() "+ ex);
            }
        }

        private void label1_Click_1(object sender, EventArgs e) {}

        private void groupBox1_Enter(object sender, EventArgs e) {}

        private void textBox2_TextChanged(object sender, EventArgs e) {}

        public Boolean validatePath(String userPath)
        //Checks validity of user input for emptiness, illegal characters or illegal path
        {
            try
            {
                if(userPath.LastIndexOf("\\") == -1) {
                    return false;
                }
                else if (string.IsNullOrEmpty(userPath))
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught during validatePath() " + ex);
                return false;
            }
        }

        public String cleanPath(String userPath)
        //Checks validity of user input and tag information for emptiness, illegal characters or illegal path values
        {
            userPath.Replace('/', ' ');
            userPath.Replace('?', ' ');
            userPath.Replace('<', ' ');
            userPath.Replace('>', ' ');
            userPath.Replace(':', ' ');
            userPath.Replace('*', ' ');
            userPath.Replace('|', ' ');
            userPath.Replace("  ", " ");
            return userPath;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DateTime startTime = DateTime.Now;

            if (!validatePath(textBox5.Text))
            {
                AppendTextBoxLine("Source path entered is invalid, skipping operation");
                goto OpsFinished;
            }
            else
            {
                AppendTextBoxLine("User input validated, starting check operation");
            }

            try
            {
                DirectoryInfo dirs = new DirectoryInfo(textBox5.Text);
                String temp = textBox5.Text;
                int rootDirCount = temp.Length - (int)temp.Replace("\\", "").Length;

                if (dirs.Exists)
                {
                    foreach (DirectoryInfo artistDirs in dirs.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        int currentDirCount = (artistDirs.FullName.Length - (int)artistDirs.FullName.Replace("\\", "").Length) - 1;

                        if (artistDirs.GetFiles("*.mp3") != null && rootDirCount == currentDirCount && artistDirs.GetDirectories().Length == 0)
                        //Caters for C:\[userpath]\[album]\[tracks]
                        {
                            checkAlbum(artistDirs);
                        }

                        foreach (DirectoryInfo albumDirs in artistDirs.GetDirectories())
                        //Caters for C:\[userpath]\[artist]\[album]\[tracks]
                        {
                            checkAlbum(albumDirs);
                        }
                    }
                }
                else
                {
                    AppendTextBoxLine("Directory does not exist");
                    goto OpsFinished;
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught" + ex);
            }

            OpsFinished:
            TimeSpan elapsed = DateTime.Now - startTime;
            AppendTextBoxLine("Operations Finished in " + elapsed.TotalSeconds.ToString("###.###") + " seconds");
            DelimitOperations();
        }

        public Boolean checkTrack(TagLib.File file)
        //Interfaces with ID3 tags to check all tags are present for track
        {
            Boolean complete = true;

            if (file.Name.Contains("INCOMPLETE~")) {
                AppendTextBoxLine("Incomplete track found " + file.Name);
                complete = false;
            }
            if (string.IsNullOrEmpty(file.Tag.Title))
            {
                AppendTextBoxLine("Title is blank for " + file.Name);
                complete = false;
            }
            if (string.IsNullOrEmpty(file.Tag.FirstArtist))
            {
                AppendTextBoxLine("Artist is blank for " + file.Name);
                complete = false;
            }
            if (string.IsNullOrEmpty(file.Tag.Album))
            {
                AppendTextBoxLine("Album is blank for " + file.Name);
                complete = false;
            }
            if (string.IsNullOrEmpty(file.Tag.FirstGenre))
            {
                AppendTextBoxLine("Genre is blank for " + file.Name);
                complete = false;
            }
            if (string.IsNullOrEmpty(file.Tag.Track.ToString()))
            {
                AppendTextBoxLine("Track No is blank for " + file.Name);
                complete = false;
            }
            return complete;
        }

        public void checkMissingTrack(int trackCount, DirectoryInfo albumDirs)
        //Checks tracks sequentially for missing or duplicates
        {
            int[] tracks = new int[trackCount];
            int i = 0;

            foreach (FileInfo songs in albumDirs.GetFiles("*.mp3"))
            {
                TagLib.File tempFile;

                try
                {
                     tempFile = TagLib.File.Create(albumDirs.FullName + "\\" + songs);
                }
                catch (CorruptFileException ex)
                {
                    AppendTextBoxLine("A corrupt file was found in " + albumDirs.FullName + " for track " + songs.FullName);
                    return;
                }

                if (i < tracks.Length)
                {
                    tracks[i] = (int)tempFile.Tag.Track;
                }
                i++;
            }

            //Filter discrepancies to top and bottom of list
            Array.Sort(tracks);

            for (int j = 0; j < tracks.Length; j++)
            {
                if ((j + 1) > tracks[j])
                {
                    AppendTextBoxLine("Duplication exists on Track " + j + " from album " + albumDirs.FullName);
                    break;
                }
                else if ((j + 1) < tracks[j])
                {
                    AppendTextBoxLine("Track " + (j + 1) + " missing from album " + albumDirs.FullName);
                    break;
                }
            }
        }

        public void checkInconsistencies(int trackCount, DirectoryInfo albumDirs)
        //Checks tracks sequentially for missing or duplicates
        {
            if (trackCount >= 1)
            {
                String[] artists = new String[trackCount];
                String[] albums = new String[trackCount];
                String[] genres = new String[trackCount];

                int i = 0;

                foreach (FileInfo songs in albumDirs.GetFiles("*.mp3"))
                {
                    TagLib.File tempFile;

                    try
                    {
                        tempFile = TagLib.File.Create(albumDirs.FullName + "\\" + songs);
                    }
                    catch (CorruptFileException ex)
                    {
                        AppendTextBoxLine("A corrupt file was found in " + albumDirs.FullName + " for track " + songs.FullName);
                        return;
                    }

                    if (i < artists.Length && i < albums.Length && i < genres.Length)
                    {
                        artists[i] = tempFile.Tag.FirstArtist;
                        albums[i] = tempFile.Tag.Album;
                        genres[i] = tempFile.Tag.FirstGenre;
                    }
                    i++;
                }

                Array.Sort(artists);
                Array.Sort(albums);
                Array.Sort(genres);

                if (artists[0] != artists[artists.Length - 1])
                {
                    AppendTextBoxLine("Inconsistencies exist in Artist for album " + albumDirs.FullName);
                }
                if (albums[0] != albums[albums.Length - 1])
                {
                    AppendTextBoxLine("Inconsistencies exist in Album for album " + albumDirs.FullName);
                }
                if (genres[0] != genres[genres.Length - 1])
                {
                    AppendTextBoxLine("Inconsistencies exist in Genre for album " + albumDirs.FullName);
                }
                if (albumDirs.ToString() != albums[0] || albumDirs.ToString() != albums[albums.Length - 1])
                {
                    AppendTextBoxLine("Album directory name doesn't match tags in folder " + albumDirs.FullName);
                }
            }
            else if (albumDirs.GetDirectories().Length == 0)
            {
                AppendTextBoxLine("No tracks in this directory " + albumDirs.FullName);
            }
        }

        public void checkAlbum(DirectoryInfo albumDirs)
        //Checks all tracks in album directory and reports status to console
        {

            int trackCount = 0;

            foreach (FileInfo tracks in albumDirs.GetFiles("*.mp3"))
            {
                TagLib.File file = null;

                try
                {
                    file = TagLib.File.Create(albumDirs.FullName + "\\" + tracks);
                }
                catch (CorruptFileException ex)
                {
                    AppendTextBoxLine("A corrupt file was found in " + albumDirs.FullName + " for track " + tracks.FullName);
                    continue;
                }

                checkTrack(file);

                if (checkBox1.Checked)
                {
                    checkArtwork(file);
                }
                trackCount++;
            }

            checkMissingTrack(trackCount, albumDirs);

            if (checkBox2.Checked)
            {
                checkInconsistencies(trackCount, albumDirs);
            }
        }

        public Boolean checkArtwork(TagLib.File file)
        //Interfaces with ID3 tags to check presence of artwork
        {
            Boolean complete = true;
            IPicture[] artwork = file.Tag.Pictures;

            if (artwork.Length == 0)
            {
                AppendTextBoxLine("Artwork not found for " + file.Name);
                complete = false;
            }
            return complete;
        }

        public void PlayMIDI(String Location)
        // Loads and plays music
        {
            try
            {
                player = new SoundPlayer(Location);
                player.PlayLooping();
            }
            catch (Exception e)
            {
                AppendTextBoxLine("Cannot find sound file " + Location + " " + e);
            }
        }

        private void stopButton_Click_1(object sender, EventArgs e)
        // Stop music
        {
            player.Stop();
            AppendTextBoxLine("Music stopped");
        }

        private void playButton_Click(object sender, EventArgs e)
        // Play music
        {
            player.PlayLooping();
            AppendTextBoxLine("Music started");
        }

        private void button3_Click(object sender, EventArgs e)
        // Apply Artwork button
        {
            DateTime startTime = DateTime.Now;

            if (!validatePath(textBox5.Text))
            {
                AppendTextBoxLine("Source path entered is invalid, skipping operation");
                goto OpsFinished;
            }
            else
            {
                AppendTextBoxLine("User input validated, starting apply artwork operation");
            }

            try
            {
                DirectoryInfo dirs = new DirectoryInfo(textBox5.Text);
                String temp = textBox5.Text;
                int rootDirCount = temp.Length - (int)temp.Replace("\\", "").Length;

                if (dirs.Exists)
                {
                    foreach (DirectoryInfo artistDirs in dirs.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        foreach (DirectoryInfo albumDirs in artistDirs.GetDirectories())
                        //Caters for C:\[userpath]\[artist]\[album]\[tracks] and recursive subdirectories
                        {
                            applyArtwork(albumDirs);
                        }
                    }
                } 
                else
                {
                    AppendTextBoxLine("Directory does not exist");
                    goto OpsFinished;
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught" + ex);
            }

            OpsFinished:
            TimeSpan elapsed = DateTime.Now - startTime;
            AppendTextBoxLine("Operations Finished in " + elapsed.TotalSeconds.ToString("###.###") + " seconds");
            DelimitOperations();
        }

        public void applyArtwork(DirectoryInfo albumDirs)
        //Checks for artwork and applies to all ID3 data on all tracks for that album
        {
            try
            {
                FileInfo picFile = new FileInfo(albumDirs.FullName + "\\" + "folder.jpg");

                if (picFile.Exists)
                {
                    Picture[] artwork = new Picture[1];
                    artwork[0] = Picture.CreateFromPath(albumDirs.FullName + "\\" + "folder.jpg");

                    foreach (FileInfo tracks in albumDirs.GetFiles("*.mp3"))
                    {
                        TagLib.File file = TagLib.File.Create(albumDirs.FullName + "\\" + tracks);
                        file.Tag.Pictures = artwork;
                        file.Save();
                    }
                }
                else
                {
                    AppendTextBoxLine("Artwork not found at " + albumDirs.FullName + "\\" + "folder.jpg");
                }
            }
            catch (Exception e)
            {
                AppendTextBoxLine("Couldn't find artwork at " + albumDirs.FullName + "\\" + "folder.jpg Exception: " + e);
            }
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e) {}

        private void button4_Click(object sender, EventArgs e)
        // Move source browse button
        {
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox2.Text = folderBrowserDialog1.SelectedPath;
            }
        }

        private void button5_Click(object sender, EventArgs e)
        // Move destination browse button
        {
            if (folderBrowserDialog2.ShowDialog() == DialogResult.OK)
            {
                this.textBox3.Text = folderBrowserDialog2.SelectedPath;
            }
        }

        private void button6_Click(object sender, EventArgs e)
        // Check source browse button
        {
            if (folderBrowserDialog3.ShowDialog() == DialogResult.OK)
            {
                this.textBox5.Text = folderBrowserDialog3.SelectedPath;
            }
        }

        private void button7_Click(object sender, EventArgs e)
        // Update case button
        {
            DateTime startTime = DateTime.Now;

            if (!validatePath(textBox5.Text))
            {
                AppendTextBoxLine("Source path entered is invalid, skipping operation");
                goto OpsFinished;
            }
            else
            {
                AppendTextBoxLine("User input validated, starting case update operation");
            }

            try
            {
                DirectoryInfo dirs = new DirectoryInfo(textBox5.Text);
                String temp = textBox5.Text;
                int rootDirCount = temp.Length - (int)temp.Replace("\\", "").Length;

                if (dirs.Exists)
                {
                    foreach (DirectoryInfo artistDirs in dirs.GetDirectories("*", SearchOption.AllDirectories))
                    {
                        int currentDirCount = (artistDirs.FullName.Length - (int)artistDirs.FullName.Replace("\\", "").Length) - 1;

                        if (artistDirs.GetFiles("*.mp3") != null && rootDirCount == currentDirCount && artistDirs.GetDirectories().Length == 0)
                        //Caters for C:\[userpath]\[album]\[tracks]
                        {
                            updateCase(artistDirs);
                        }

                        foreach (DirectoryInfo albumDirs in artistDirs.GetDirectories())
                        //Caters for C:\[userpath]\[artist]\[album]\[tracks]
                        {
                            updateCase(albumDirs);
                        }
                    }
                }
                else
                {
                    AppendTextBoxLine("Directory does not exist");
                    goto OpsFinished;
                }
            }
            catch (Exception ex)
            {
                AppendTextBoxLine("Exception caught" + ex);
            }

            OpsFinished:
            TimeSpan elapsed = DateTime.Now - startTime;
            AppendTextBoxLine("Operations Finished in " + elapsed.TotalSeconds.ToString("###.###") + " seconds");
            DelimitOperations();
        }

        private void textBox5_TextChanged(object sender, EventArgs e) {}

        private void textBox3_TextChanged(object sender, EventArgs e) {}

        public void updateCase(DirectoryInfo albumDirs)
        // Updates all ID3 track information in an album directory and reports status to console
        {
            foreach (FileInfo tracks in albumDirs.GetFiles("*.mp3"))
            {
                TagLib.File file = TagLib.File.Create(albumDirs.FullName + "\\" + tracks);
                String[] artist = new String[1];
                String[] genre = new String[1];

                artist[0] = capitaliseWords(cleanTag(file.Tag.FirstArtist.ToString()));
                genre[0] = capitaliseWords(cleanTag(file.Tag.FirstGenre.ToString()));

                file.Tag.Title = capitaliseWords(cleanTag(file.Tag.Title));
                file.Tag.Artists = artist;
                file.Tag.Album = capitaliseWords(cleanTag(file.Tag.Album));
                file.Tag.Genres = genre;
                file.Save();
            }
            AppendTextBoxLine("Case updated for album " + albumDirs.FullName);
        }

        public static string capitaliseWords(string value)
        // Capitalises all words in a string
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }
            StringBuilder result = new StringBuilder(value);
            result[0] = char.ToUpper(result[0]);
            for (int i = 1; i < result.Length; ++i)
            {
                if (char.IsWhiteSpace(result[i - 1]))
                    result[i] = char.ToUpper(result[i]);
            }
            return result.ToString();
        }

        public String cleanTag(String tag)
        // Tidies emptiness and special characters (except , and .) from ID3 tag
        {
            if (String.IsNullOrEmpty(tag))
            {
                return "";
            }
            tag = Regex.Replace(tag,"[^\\w\\.,'-]"," ");
            tag = Regex.Replace(tag, @"\s+", " ");
            tag = tag.Trim();
            return tag;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) {}

        private void checkBox2_CheckedChanged(object sender, EventArgs e) {}

        private void trackBar1_Scroll(object sender, EventArgs e)
        // Volume control
        {
            // Calculate the volume that's being set
            int NewVolume = ((ushort.MaxValue / 10) * trackBar1.Value);
            // Set the same volume for both the left and the right channels
            uint NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            // Set the volume
            waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ClearTextBox();
            WriteConsoleHeaders();
        }
    }
}
