using System;
using System.Collections.Generic;

using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using HtmlAgilityPack;
using System.Net;
using System.IO;
using System.Data.OleDb;
using System.Text.RegularExpressions;
using Microsoft.CSharp.RuntimeBinder;
using System.Configuration;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        

        //List<Missionary> missionaries = new List<Missionary>();
        MissionaryManager missionaryManagerChristensen; // I, Anz. Christensen, am organizing all the missionaries.
        TrainManager conductorChristensen; // As well as the trains. Thus, they are named after myself. :) 
        ExcelManager excelManagerChristensen; // And so we may as well continue the trend...
        int nextMissionaryID = 0;


        public Form1()
        {
            InitializeComponent();
            //listBox1.Items = new String[] { "Kenneth", "Christensen" };
        }

        
        // Will take the Missionaries from MissioanryManager and work with the TrainManager to schedule things.
        public void Schedule_Trips()
        {
            missionaryManagerChristensen.calculateTrips();
            //for (int i = 0; i < nextMissionaryID - 1; i++) // Cycle through missionarys in the Manager by ID.
            //{
            //    managerChristensen.calculateTrip(
            //}
        }

        // Will build the MissionaryManager from the Excel File
        public void Analyze_File(String fileURI, String sheetName, String outputFileName)
        {
            

            DateTime selectedDate = monthCalendar1.TodayDate;
            String selectedDateString = FUNCS.twoDigitHour(selectedDate.Day) + "-" + FUNCS.twoDigitHour(selectedDate.Month) + "-" + selectedDate.Year;
            selectedDateString = "26-08-2015";
            
            //conductorChristensen = new TrainManager(selectedDateString);
            //missionaryManagerChristensen = new MissionaryManager(conductorChristensen);
            excelManagerChristensen = new ExcelManager(selectedDateString);

            //var fileName = string.Format("{0}\\fileNameHere", Directory.GetCurrentDirectory());
            excelManagerChristensen.Import_Travel_File(fileURI, sheetName);
            conductorChristensen = excelManagerChristensen.getConductor();
            missionaryManagerChristensen = excelManagerChristensen.getMissionaryManager();

            //int x = conductorChristensen.getTrainTimes("Milano Centrale", "Torino Porta Susa", "09-06-2015", "06");
            missionaryManagerChristensen.printAllMissionaries();
            Console.WriteLine("About to calculate!");
            missionaryManagerChristensen.calculateTrips();
            Console.WriteLine("calculating");
            missionaryManagerChristensen.printAllMissionaries();
            missionaryManagerChristensen.printAllMissionarySchedules();
            excelManagerChristensen.write_to_text_file("C:\\Users\\SEC2016583\\Desktop",outputFileName,missionaryManagerChristensen.getAllMissionarySchedules());
            Console.Beep();

            FUNCS.ShowPromptBox("Finished running", "You should look on the desktop for your file.", "I mean nothing right now.");
            

            //Console.WriteLine(data.Columns);

        }


        // Runs when you click submit file.
        private void submitFile(object sender, EventArgs e)
        {
            string f = textBox1.Text;
            string s = textBox2.Text;
            string o = textBox3.Text;

            
            Properties.Settings.Default.inputFile = f;
            Properties.Settings.Default.sheetName = s;
            Properties.Settings.Default.outputFile = o;

            Properties.Settings.Default.Save();

            Console.WriteLine("getting info in excel file at " + f+", in sheet "+s);
            Analyze_File(f,s,o);
        }

        // Runs when you click browse.
        private void showFileBrowser(object sender, EventArgs e)
        {
            //int size = -1;
            DialogResult result = openFileDialog1.ShowDialog(); // Show the dialog.
            if (result == DialogResult.OK) // Test result.
            {
                string file = openFileDialog1.FileName;
                textBox1.Text = file;
                /*try
                {
                    string text = File.ReadAllText(file);
                    size = text.Length;
                }
                catch (IOException)
                {
                }
            }
            Console.WriteLine(size); // <-- Shows file size in debugging mode.
            Console.WriteLine(result); // <-- For debugging use.
                 * */
            }
        }
        
        
        private void Analyze_File2(object sender, EventArgs e)
        {
            //Analyze_File("nothing");
        }



    }

    public class Train
    {
        public String depart { get; set; } // Departure City
        public String arrive { get; set; } // Arrival City
        public int trainNumber { get; set; } // Train number
        public String date { get; set; } // Date of train.
        public int timeMinutes { get; set; } // Departure time in minutes since 00:00.
        public int arrivalTimeMinutes { get; set; } // ARrival time in minutes since 00:00.
        public int durationMinutes { get; set; } // Train ride duration in minutes
        public int costEuro { get; set; } // Cost of ticket in Euro
        public int type { get; set; } // Type of train
        

        public Train(String d, String a, String dt, int tN, int tM, int aTM, int dM, int cE, int t){
            depart = d;
            arrive = a;
            date = dt;
            trainNumber = tN;
            timeMinutes = tM;
            arrivalTimeMinutes = aTM;
            durationMinutes = dM;
            costEuro = cE;
            type = t;
        }
    }

    public class TrainManager
    {
        public Dictionary<String, String> stationsSettings;
        public List<String> stations;
        public String[] hubs;
        List<Train> trains;
        DataSet trainDB;
        DataTable orariTable;
        DataColumn trainID;

        String day;
        String transferDay;
        String previousDay;

        private const String noDayParameter = "none";

        private String checkDayParameter(String s)
        {
            if (s.Equals(noDayParameter)){
                return day;
            } else {
                return s;
            }
        }

        public String[] getHubs()
        {
            return hubs.ToArray();
        }
        public Train getPreviousTrain(Train t)
        {
            String depart = t.depart;
            String arrive = t.arrive;
            int arrivalTimeMinutes = t.arrivalTimeMinutes;
            DataRow[] options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND arrivalTimeMinutes < " + arrivalTimeMinutes.ToString());
            int eBrake = 0;
            int nextTime = arrivalTimeMinutes;
            while (options.Length == 0) // Spero che quando non ci sono treni adeguati che ritorna null, invece di un array vuoto....
            {
                eBrake = eBrake + 1;
                Console.WriteLine("next time was: " + nextTime.ToString());
                nextTime = nextTime - (10 * eBrake);
                getTrainTimes(depart, arrive, day, FUNCS.minutesToTimeString(nextTime));
                Console.WriteLine("next time is: " + nextTime.ToString());
                options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND arrivalTimeMinutes < " + arrivalTimeMinutes.ToString());
                if (eBrake > 6 || nextTime < 1)
                {
                    //getTrainTimes(depart, arrive, day, FUNCS.minutesToTimeString(nextTime));
                    return null;
                }
            }
            // Return last train.
            DataRow choice = options[options.Length-1];
            return new Train(choice["depart"].ToString(), choice["arrive"].ToString(), choice["date"].ToString(), Int32.Parse(choice["trainNumber"].ToString()), Int32.Parse(choice["timeMinutes"].ToString()), Int32.Parse(choice["arrivalTimeMinutes"].ToString()), Int32.Parse(choice["durationMinutes"].ToString()), Int32.Parse(choice["costEuro"].ToString()), Int32.Parse(choice["type"].ToString()));
            
        }

        public Train getNextTrain(Train t)
        {
            String depart = t.depart;
            String arrive = t.arrive;
            int timeMinutes = t.timeMinutes;
            DataRow[] options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND timeMinutes > " + timeMinutes.ToString());
            int eBrake = 0;
            int nextTime = timeMinutes;
            while (options.Length == 0) // Spero che quando non ci sono treni adeguati che ritorna null, invece di un array vuoto....
            {
                eBrake = eBrake + 1;
                Console.WriteLine("next time was: " + nextTime.ToString());
                nextTime = nextTime + (10 * eBrake);
                getTrainTimes(depart, arrive, day, FUNCS.minutesToTimeString(nextTime));
                Console.WriteLine("next time is: " + nextTime.ToString());
                options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND timeMinutes < " + timeMinutes.ToString());
                if (eBrake > 6)
                {
                    //getTrainTimes(depart, arrive, day, FUNCS.minutesToTimeString(nextTime));
                    return null;
                }
            }
            // Return first train.
            DataRow choice = options[0];
            return new Train(choice["depart"].ToString(), choice["arrive"].ToString(), choice["date"].ToString(), Int32.Parse(choice["trainNumber"].ToString()), Int32.Parse(choice["timeMinutes"].ToString()), Int32.Parse(choice["arrivalTimeMinutes"].ToString()), Int32.Parse(choice["durationMinutes"].ToString()), Int32.Parse(choice["costEuro"].ToString()), Int32.Parse(choice["type"].ToString()));

        }

        #region unusedMethods
        public Train getTrain(String origin, String destination, int time, String usedDay = "none")
        {
            // TODO: Get a train by looking through our DB, adding to it if necessary, calling getSchedule
            return null;
        }

        public Train getSchedule(String origin, String destination, int time, String usedDay = "none")
        {
            // TODO: Download more schedule with the given info.
            return null;
        }
        
        #endregion unusedMethods

        public void saveSettings()
        {
            JavaScriptSerializer jsoner = new JavaScriptSerializer();
            Properties.Settings.Default.Cities = jsoner.Serialize(stationsSettings);
            Properties.Settings.Default.Save();
            
        }

        public TrainManager(String d)
        {
            day = d;
            trains = new List<Train>();
            String fromResources = WindowsFormsApplication1.Properties.Resources.Stations;

            Regex getStations = new Regex(@"""([^""]|\\"")*"""); // Will grab every step
            String[] stationsArray = getStations.Matches(fromResources).Cast<Match>().Select(m => m.Value).ToArray();

            
            JavaScriptSerializer jsoner = new JavaScriptSerializer();
            String JSONdict = Properties.Settings.Default.Cities;
            stationsSettings = jsoner.Deserialize<Dictionary<String, String>>(JSONdict);
            if (stationsSettings == null)
            {
                stationsSettings = new Dictionary<string, string>();
            }

            for (int z = 0; z < stationsArray.Length; z++ )
            {
                stationsArray[z] = stationsArray[z].Replace("\"", "");
            }
            stations = stationsArray.ToList<String>();
            hubs = new String[WindowsFormsApplication1.Properties.Settings.Default.hubs.Count];
            WindowsFormsApplication1.Properties.Settings.Default.hubs.CopyTo(hubs,0);
            foreach (string h in hubs)
            {
                Console.WriteLine(validateStation(h));
            }
            
            trainDB = new DataSet("trains");
            orariTable = trainDB.Tables.Add("orari");
            Type x = typeof(Int32);
            trainID = orariTable.Columns.Add("trainNumber", x);
            orariTable.Columns.Add("depart", typeof(String));
            orariTable.Columns.Add("arrive", typeof(String));
            orariTable.Columns.Add("date", typeof(String));
            orariTable.Columns.Add("timeMinutes", x);
            orariTable.Columns.Add("arrivalTimeMinutes", x);
            orariTable.Columns.Add("durationMinutes", x);
            orariTable.Columns.Add("costEuro", x);
            orariTable.Columns.Add("type", x);
            //orariTable.PrimaryKey = new DataColumn[] { trainID };

            UniqueConstraint distinctIDArriveDepart = new UniqueConstraint(new DataColumn[] { orariTable.Columns["trainNumber"], orariTable.Columns["arrive"], orariTable.Columns["depart"], orariTable.Columns["date"] });
            orariTable.Constraints.Add(distinctIDArriveDepart);
        }

        public Train getTrainToArriveAt(String depart, String arrive, int arrivalTimeMinutes, String usedDay = noDayParameter, bool sleepover = false)
        {
            // Check if the default parameter is left this.noDayParameter, if so, make it = this.day.
            usedDay = checkDayParameter(usedDay);

            depart = validateStation(depart);
            arrive = validateStation(arrive);
            String queryString = String.Format("depart ='{0}' AND arrive = '{1}' AND arrivalTimeMinutes < {2} AND date = '{3}'",depart, arrive, arrivalTimeMinutes, usedDay);
            DataRow[] options = orariTable.Select(queryString, "arrivalTimeMinutes DESC");
            //DataRow[] options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND arrivalTimeMinutes < " + arrivalTimeMinutes.ToString());
            int eBrake = 0;
            int nextTime = arrivalTimeMinutes;
            bool firstTime = true;
            while (options.Length == 0 || (firstTime && options.Length > 0 && ((20*60+30) - Int32.Parse(options[0]["arrivalTimeMinutes"].ToString())) > (60 * 1)))  // Spero che quando non ci sono treni adeguati che ritorna null, invece di un array vuoto....
            {
                
                eBrake = eBrake + 1;
                Console.WriteLine("next time was: " + nextTime.ToString());
                nextTime = nextTime - (10 * eBrake);
                if (options.Length > 0 && firstTime)
                {
                    nextTime = nextTime - 90; // If we're not leaving LATE enough, because of a sleepover, we need to give ourselves plenty of time.
                }
                if (options.Length > 0)
                {
                    firstTime = false;
                }
                getTrainTimes(depart, arrive, usedDay, FUNCS.minutesToTimeString(nextTime));
                Console.WriteLine("next time is: " + nextTime.ToString());
                String queryString2 = String.Format("depart ='{0}' AND arrive = '{1}' AND arrivalTimeMinutes < {2} AND date = '{3}'", depart, arrive, arrivalTimeMinutes, usedDay);
                options = orariTable.Select(queryString2, "arrivalTimeMinutes DESC");
                //options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND arrivalTimeMinutes < " + arrivalTimeMinutes.ToString());
                if (eBrake > 6)
                {
                    //getTrainTimes(depart, arrive, day, FUNCS.minutesToTimeString(nextTime));
                    return null;
                }
                
            }
            // Naive... return the last one, we'll optimize later.
            
            DataRow choice = options[0];
            
            
            if (sleepover) // if we're sleeping over, return the last possible solution.
            {
                choice = options[options.Length - 1];
            }
            
            return new Train(choice["depart"].ToString(), choice["arrive"].ToString(), choice["date"].ToString(), Int32.Parse(choice["trainNumber"].ToString()), Int32.Parse(choice["timeMinutes"].ToString()), Int32.Parse(choice["arrivalTimeMinutes"].ToString()), Int32.Parse(choice["durationMinutes"].ToString()), Int32.Parse(choice["costEuro"].ToString()), Int32.Parse(choice["type"].ToString()));
            
            
        }

        public Train getTrainToLeaveAt(String depart, String arrive, int timeMinutes, String usedDay = noDayParameter)
        {
            if (depart.Equals(arrive))
            {
                Console.WriteLine("This coppia doesn't need travel: " + depart);
                return null;
            }
            // Check if the default parameter is left this.noDayParameter, if so, make it = this.day.
            usedDay = checkDayParameter(usedDay);
            String queryString = String.Format("depart ='{0}' AND arrive = '{1}' AND timeMinutes < {2} AND date = '{3}'", depart, arrive, timeMinutes, usedDay);
            DataRow[] options = orariTable.Select(queryString);
            //DataRow[] options = orariTable.Select("depart = '" + depart + "' AND arrive = '" + arrive + "' AND timeMinutes < " + timeMinutes.ToString());
            int eBrake = 0;
            int nextTime = timeMinutes;
            while (options == null || options.Length == 0) // Spero che quando non ci sono treni adeguati che ritorna null, invece di un array vuoto....
            {
                if (depart == "Firenze S. M. Novella" && arrive == "Milano Centrale")
                {
                    int x = 42; // gimmi a break point...
                }
                getTrainTimes(depart, arrive, usedDay, FUNCS.minutesToTimeString(nextTime -= (10 * eBrake)));

                String queryString2 = String.Format("depart ='{0}' AND arrive = '{1}' AND timeMinutes < {2} AND date = '{3}'", depart, arrive, timeMinutes, usedDay);
                
                //String query = "depart = '" + depart + "' AND arrive = '" + arrive + "' AND timeMinutes < " + timeMinutes.ToString();
                Console.WriteLine(queryString2);
                options = orariTable.Select(queryString2);


                if (eBrake++ > 6)
                {
                    return null;
                }
            }
            // Naive... return the first one, we'll optimize later.
            DataRow choice = options[0];
            return new Train(choice["depart"].ToString(), choice["arrive"].ToString(), choice["date"].ToString(), Int32.Parse(choice["trainNumber"].ToString()), Int32.Parse(choice["timeMinutes"].ToString()), Int32.Parse(choice["arrivalTimeMinutes"].ToString()), Int32.Parse(choice["durationMinutes"].ToString()), Int32.Parse(choice["costEuro"].ToString()), Int32.Parse(choice["type"].ToString()));

            
        }

        // Will add to the DB, returning an int 0 based on success, an error code otherwise.
        public int getTrainTimes(String departure, String arrival, String date, String time)
        {
            if (departure.Equals(arrival))
            {
                return -1;
            }

        

            Console.WriteLine(departure + ", " + arrival + ", " + date + ", " + time);
            // Return object:
            //String[][] times = new string[10][]; // Makes space for 10 solutions of 2 times each.
            
            /***
             * Info we want to add to DB
             * 
             * 1) Station of Departure
             * 2) Station of Arrival
             * 3) Time of Departure
             * 4) Time of Arrival
             * 5) Date of trip
             * 6) Trip duration (why not)
             * 7) Train number
             * 8) Train type
             * 9) Ticket Cost
             * 
             **/
            String stationD = validateStation(departure); 
            String stationA = validateStation(arrival);
            // Stations were wrong. Return an error code.
            if ((stationD.Equals("error") || stationA.Equals("error")) == true)
            {
                return ERROR.INVALID_STATION;
            }

            int timeD = 0; // minutes from midnight
            int timeA = 0; // minutes from midnight
            String dateT; // mm-dd-yyyy format string
            int duration; // minutes
            int trainNum;
            int trainType; // Comes from TRAIN_TYPES class constant. See below.
            int trainCost;

            Dictionary<String, String> postArgs = new Dictionary<string, string>(){
                {"url_desktop", "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it"},
                {"url_mobile", "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it"},
                {"tripType", "on"},
                {"isRoundTrip", "false"},
                {"departureStation", "Milano Centrale"},
                {"arrivalStation", "Torino Porta Susa"},
                {"departureDate", "06-06-2015"},
                {"returnDate", "06-06-2015"},
                {"ynFlexibleDates", "off"},
                {"selectedTrainType", "tutti"},
                {"selectedTrainClassifications", ""}
            };

            postArgs["departureStation"] = departure;
            postArgs["arrivalStation"] = arrival;
            postArgs["departureDate"] = date;
            postArgs["departureTime"] = FUNCS.twoDigitHour(time).Split(new char[]{':'})[0];

            string post_data = createPostDataString(postArgs);
            //Console.WriteLine(post_data);
            string uri = "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            //request.AllowAutoRedirect = false;
            request.CookieContainer = new CookieContainer(); // Handles the redirects, etc.

            byte[] postBytes = Encoding.ASCII.GetBytes(post_data);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;
            Stream requestStream = request.GetRequestStream();

            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            String html = new StreamReader(response.GetResponseStream()).ReadToEnd();
            //Console.WriteLine(html);

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            var root = htmlDoc.DocumentNode;
            int counterSR = 0;
            int counterT = 0;

            #region parseResponse

            #region checkForDateChange

            List<String> train_dates = new List<String>();
            String currentDay = date;
            Regex dateRegex = new Regex(@"\d\d\-\d\d-\d\d\d\d");


            foreach (HtmlNode n in root.SelectNodes("//*[contains(@class, 'panel')]/div"))
            {
                String id = n.GetAttributeValue("id","false");
                if (id.Substring(0, id.Length - 1).Equals("travelSolution"))
                {
                    // it's a solutionRow, we need to add its date to the train_dates.
                    train_dates.Add(currentDay.ToString());
                    
                }
                else if (id.Equals("separator"))
                {
                    // it's a new date. use the one listed.
                    currentDay = n.SelectSingleNode(".//table/thead/tr/th").InnerText.Trim();
                    Console.WriteLine("changed current date to....");
                }

            }

            // At this point, train_dates should be a list of dates corresponding to each solution row, so we can add each solutions date to the db.
            #endregion

            int solutionNumber = 0;

            foreach (HtmlNode node in root.SelectNodes("//*[contains(@class,'solutionRow')]"))
            {

                String[] currentTime = new string[2];

                //Console.WriteLine("Solution " + counterSR.ToString());
                // Get the times:
                foreach (HtmlNode node2 in node.SelectNodes(".//td/div/span[contains(@class, 'time')]"))
                {
                    string thisTime = node2.InnerHtml.Trim();
                    if (counterT % 2 == 0)
                    {
                        //Console.WriteLine("Depart: " + thisTime);
                        //currentTime[0] = thisTime;
                        timeD = FUNCS.timeStringToMinutes(thisTime);
                    }
                    else
                    {
                        //Console.WriteLine("Arrive: " + thisTime);
                        //currentTime[1] = thisTime;
                        timeA = FUNCS.timeStringToMinutes(thisTime);
                    }
                    counterT++;

                }

                duration = timeA - timeD;
                // Wait, I don't need this... I just calculate it from my own times. Done above.
                /*
                // Get the duration:
                counterT = 0;
                foreach (HtmlNode node2 in node.SelectNodes(".//td/div/span[contains(@class, 'bottom')]"))
                {
                    string thisStation = node2.InnerHtml.Trim();
                    if (counterT % 2 == 0)
                    {
                        Console.WriteLine("Depart: " + thisStation);
                        //currentTime[0] = thisTime;
                        timeD = FUNCS.timeStringToMinutes(thisStation);
                    }
                    else
                    {
                        Console.WriteLine("Arrive: " + thisStation);
                        //currentTime[1] = thisTime;
                        timeA = FUNCS.timeStringToMinutes(thisStation);
                    }
                    counterT++;

                }*/

                
                // Get the Type/Number:

                String descT = node.SelectNodes(".//td/div/div/div[contains(@class, 'descr')]")[0].InnerText;//.Split(new char[] { ' ' });
                //String trainTypeName = desc[0];

                Regex descRX = new Regex(@"[^\t\r\n\v\f]+"); // Will grab everything that's not whitespace...
                String[] desc = descRX.Matches(descT).Cast<Match>().Select(m => m.Value).ToArray();

                String trainTypeName = desc[0];
                trainType = FUNCS.getTrainTypeCode(trainTypeName);
                if (trainTypeName == "Autobus")
                {
                    trainNum = -11;
                }
                else
                {
                    if (desc[1].Equals("Urb"))
                    {
                        desc[1] = "0";
                    }
                        trainNum = Int32.Parse(desc[1]);
                        
                }
                
                // Get the Cost:

                try
                {
                    String costString = node.SelectNodes(".//td/span/div/span[contains(@class, 'price')]")[0].InnerText;
                    Regex costRX = new Regex(@"[0-9]+");
                    String cx = costRX.Matches(costString).Cast<Match>().Select(m => m.Value).ToArray()[0];
                    String cy = cx.Replace(",", ".");
                    trainCost = Int32.Parse(cy);
                    
                }
                catch (Exception)
                {
                    trainCost = -1;
                }

            #endregion parseResponse

                //times[counterSR] = (String[])currentTime.Clone();
                // Here we add what we've gotten from the node to the DB, if valid.

                DataRow currentSolution = orariTable.NewRow();
                
                currentSolution["depart"] = stationD;
                currentSolution["arrive"] = stationA;
                currentSolution["trainNumber"] = trainNum;
                currentSolution["date"] = train_dates[solutionNumber++];
                currentSolution["timeMinutes"] = timeD;
                currentSolution["durationMinutes"] = duration;
                currentSolution["arrivalTimeMinutes"] = timeA;
                currentSolution["costEuro"] = trainCost;
                currentSolution["type"] = trainType;

                try
                {
                    if (trainNum != -11) // if it's not a bus.
                    {
                        orariTable.Rows.Add(currentSolution);
                    }
                }
                catch (ConstraintException e)
                {
                    Console.WriteLine(currentSolution["trainNumber"]);
                    DataRow choice = orariTable.Select("trainNumber = " + currentSolution["trainNumber"].ToString())[0];
                    Train error = new Train(choice["depart"].ToString(), choice["arrive"].ToString(), choice["date"].ToString(), Int32.Parse(choice["trainNumber"].ToString()), Int32.Parse(choice["timeMinutes"].ToString()), Int32.Parse(choice["arrivalTimeMinutes"].ToString()), Int32.Parse(choice["durationMinutes"].ToString()), Int32.Parse(choice["costEuro"].ToString()), Int32.Parse(choice["type"].ToString()));
                    if (error.depart.Equals("Firenze S. M. Novella") && error.arrive.Equals("Milano Centrale"))
                    {
                        bool breaker = true;
                    }
                }
                counterSR++;

            }


            Console.WriteLine("We got " + counterT.ToString() + " times in " + counterSR.ToString() + " solutions.");
            return 0;
        }
   
        public String validateStation (String station){
            String s = station.Trim();
            //try
            //{
            //    s = Properties.Settings.Default[station].ToString();
            //}
            //catch (SettingsPropertyNotFoundException)
            //{
            //    s = "error";
            //}

            if (stationsSettings.ContainsKey(station))
            {
                s = stationsSettings[station];
            }

            if (stations.Contains(s))
            {
                return s;
            }
            else
            {
                String newStationSetting = FUNCS.ShowPromptBox("Invalid train station", "What station does '" + station + "' actually mean (Look up on trenitalia.com if you don't know)?", station);

                stationsSettings[station] = newStationSetting;
                saveSettings();
                return validateStation(newStationSetting);
               
            }

            //if (s.Equals("error")){
            //    //String withQuotes = "\"" + station + "\"";
            //    //withQuotes = "\"AACHEN HBF\"";
                
            //    if (stations.Contains(station)){
            //        return station; // theh passed name is a legitimate option. Give it back.
                    
            //    } else {
            //        //return "error"; // the passed name is not a legitmate option, and they haven't told us what it should be. Maybe we could ask for it? Later.
            //        String newStationSetting = FUNCS.ShowPromptBox("What station does '"+station+"' actually mean (Look up on trenitalia.com if you don't know)?", "Invalid train station", station);
            //        var property = new SettingsProperty(Properties.Settings.Default.Properties["Milano"]);
                    
            //        property.Name = station;
            //        Properties.Settings.Default.Properties.Add(property);
            //        Properties.Settings.Default[station] = newStationSetting;
            //        Properties.Settings.Default.Save();
            //        return newStationSetting;
            //    }
            //} 

            return s; // return the station which corresponds to the one passed.


        }

        
        public String createPostDataString(Dictionary<String, String> args)
        {
            String returnValue = "";
            String[] keys = args.Keys.ToArray();
            String[] values = args.Values.ToArray();
            int j = args.Keys.ToArray().Length;
            for (int i = 0; i < j; i++)
            {
                returnValue = returnValue + Uri.EscapeUriString(keys[i]) + "=" + Uri.EscapeUriString(values[i]);
                if (i != j){
                    returnValue = returnValue + "&";
                }
            }
            return returnValue;
        }

    }

    public class Missionary
    {
        public String fullName { get; set; }
        public String name {get; set;}
        public int id { get; set; }
        //public String[] legs { get; set; }
        public String[][] comps { get; set; }
        public Leg[] legs { get; set; }
        public int[] legIndexs { get; set; }
        public MissionaryManager manager { get; set; }
        public String home { get; set; }
        public String zone { get; set; }

        public String ToString()
        {
            String sB = "";
            foreach (Leg cl in legs){
                string s = "";
                if (cl.train != null)
                {
                    s = cl.train.depart;
                }
                
                sB = sB+" "+s;
            }
            String cB = "";
            foreach (String[] a in comps)
            {
                cB = cB + " (";
                foreach (String b in a)
                {
                    cB = cB + ", " + b;
                }
                cB = cB + ")";
            }
            return fullName + "; " + name + "; " + zone + "; " + id.ToString() + ", " + sB+"; "+cB;
        }

        public int getTravelTime()
        {
            return legs[legs.Length - 1].time_end - legs[0].time_start;
        }

        public string getSchedule()
        {
            string scheduleResponse = "";
            foreach (Leg l in legs)
            {
                if (l.train != null)
                {
                    scheduleResponse = scheduleResponse + l.train.date + ": " + FUNCS.minutesToTimeString(l.train.timeMinutes) + "-" + FUNCS.minutesToTimeString(l.train.arrivalTimeMinutes) + "      " + l.train.depart + " -> " + l.train.arrive + "(" + TRAIN_TYPES.getTypeFromNumber(l.train.type)+": "+ l.train.trainNumber+")"+"\r\n";
                    
                }
                else
                {
                    scheduleResponse = scheduleResponse + "No train found for trip from " + l.depart + " to " + l.arrive + ".\r\n";
                }
            }
            return scheduleResponse;
        }

        // Checks through the legs, and returns the time that we will be leaving from String d.
        public int getTimeToEndLeg(String d)
        {
            for (int i = 0; i < legs.Length; i++)
            {
                Leg l = legs[i];
                Train t = l.train;

                // We've found the train we're leaving on....
                if (t != null && t.depart.Equals(d))
                {
                    if (l.skip == true)
                    {
                        if (i > 0 && legs[i - 1].train != null)
                        {
                            return legs[i - 1].train.arrivalTimeMinutes;
                        }
                        else
                        {
                            return manager.preferredFinish;
                        }

                    }

                    // But if we're sleeping at our destination, we actually need to get to bed on time...
                    if (i > 0 && legs[i - 1].going_to_sleepover)
                    {
                        Console.WriteLine(String.Format("Returning the preferred finish time ({0}) while going to {1}.", manager.preferredFinish, d));
                        return manager.preferredFinish;
                    }

                    return t.timeMinutes;
                }
                else
                {
                    return manager.preferredFinish;
                }
            }

            return -1;
        }

        // Checks through the legs, and returns the time that we will be arriving at station String d
        public int getReadyTimeAt(String d)
        {
            for (int i = 0; i < legs.Length-1; i++)
            {
                if (legs[i].train != null && legs[i].train.arrive.Equals(d))
                //if (legs[i+1].train != null && legs[i + 1].train.depart.Equals(d))
                { // Then legs[i] is the leg we're looking for, because it arrives at said station.
                    if (legs[i].skip == true)
                    {
                        if (i > 0 && legs[i - 1].train != null && legs[i - 1].train.arrivalTimeMinutes != null)
                        {
                            return legs[i - 1].train.arrivalTimeMinutes+60;
                        }
                        else
                        {
                            return manager.preferredStart+60;
                        }
                    }
                    // If we just got done with a sleepover, we need to get going in the morning though.
                    if (legs[i].going_to_sleepover)
                    {
                        Console.WriteLine(String.Format("Returning the preferred finish time ({0}) while going to {1}.", manager.preferredFinish, d));
                        return manager.preferredStart;
                    }

                    return legs[i].train.arrivalTimeMinutes;
                }
            }
            
            return 11*60; // Say eleven oclock for now. TODO
        }
        

        public int fillLeg(Train t)
        {
            bool skippy = false;
            if (t == null)
            {
                skippy = true;
                t = new Train("nowhere", "here", "mai", -1, -1, -1, -1, -1, -1); // We've got to do something... I suppose there are better ways to do this, but...
            }

            String d = t.depart;
            String a = t.arrive;
            int index = new int();
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i].depart.Equals(d) && legs[i].arrive.Equals(a) && !skippy)
                {
                    legs[i].train = t;
                    legs[i].time_start = t.timeMinutes;
                    legs[i].time_end = t.arrivalTimeMinutes;
                    if (i > 0 && ((legs[i].time_start < legs[i - 1].time_end + manager.minLayover)&&(legs[i].date.Equals(legs[i-1].date))))
                    {
                        legs[i].conflict_before = true;
                        legs[i - 1].conflict_after = true;
                    }
                    else
                    {
                        if (i > 0)
                        {
                            legs[i].conflict_before = false;
                            legs[i - 1].conflict_after = false;
                        }
                    }
                    if (i < legs.Length - 1 && ((legs[i].time_end > legs[i + 1].time_start - manager.minLayover)&&(legs[i].date.Equals(legs[i+1].date))))
                    {
                        legs[i].conflict_after = true;
                        legs[i + 1].conflict_before = true;
                    }
                    else
                    {
                        if (i < legs.Length - 1)
                        {
                            legs[i].conflict_after = false;
                            legs[i + 1].conflict_before = false;
                        }
                    }
                    index = i;
                    break;

                }
                else if (legs[i].depart.Equals(d) && legs[i].arrive.Equals(a) && skippy)
                {
                    legs[i].skip = skippy; // We don't know how to get answer for this train, skip it.
                    legs[i].train = null;
                }
                else
                {
                    //Console.WriteLine("This comp has no such train!");
                }
            }

            // Different from setLeg
            String[] cs = legs[index].comps;
            foreach (string c in cs){
                Missionary thisComp = manager.getMissionaryByName(c);
                if (thisComp != null)
                {
                    thisComp.setLeg(t);
                }
            }

            return -1;
        }

        // Does the exact same thing as fillLeg, but not for every companion. Called by fillLeg.
        public int setLeg(Train t)
        {
            bool skippy = false;
            if (t == null)
            {
                skippy = true;
                t = new Train("nowhere", "here", "mai", -1, -1, -1, -1, -1, -1); // We've got to do something... I suppose there are better ways to do this, but...
            }
            String d = t.depart;
            String a = t.arrive;
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i].depart.Equals(d) && legs[i].arrive.Equals(a))
                {
                    legs[i].train = t;
                    legs[i].time_start = t.timeMinutes;
                    legs[i].time_end = t.arrivalTimeMinutes;
                    if (i > 0 && ((legs[i].time_start < legs[i - 1].time_end + manager.minLayover) && legs[i].date.Equals(legs[i-1].date)) && !skippy)
                    {
                        legs[i].conflict_before = true;
                        legs[i - 1].conflict_after = true;
                    }
                    else
                    {
                        if (i > 0)
                        {
                            legs[i].conflict_before = false;
                            legs[i - 1].conflict_after = false;
                        }
                    }
                    if (i < legs.Length - 1 && ((legs[i].time_end > legs[i + 1].time_start - manager.minLayover) && legs[i].date.Equals(legs[i+1].date)))
                    {
                        legs[i].conflict_after = true;
                        legs[i + 1].conflict_before = true;
                    }
                    else
                    {
                        if (i < legs.Length - 1)
                        {
                            legs[i].conflict_after = false;
                            legs[i + 1].conflict_before = false;
                        }
                    }
                    return i;

                }
                else if (legs[i].depart.Equals(d) && legs[i].arrive.Equals(a) && skippy)
                {
                    legs[i].skip = skippy; // We don't know how to get answer for this train, skip it.
                    legs[i].train = null;
                }
                else
                {
                    //Console.WriteLine("this comp has no such train!!");
                }
            }
            return -1;
        }

        public String[] getStops()
        {
            List<String> stops = new List<String>();
            for (int i = 0; i<legs.Length; i++){
                stops.Add(legs[i].depart);
            }
            stops.Add(legs[legs.Length-1].arrive);
            return stops.ToArray();
        }

        public Missionary(String fN, String n, int i, String z, Leg[] l, String[][] c, Leg[] p, int[] lI, MissionaryManager mm)
        {
            fullName = fN;
            name = n;
            id = i;
            legs = l;
            comps = c;
            legs = p;
            legIndexs = lI;
            manager = mm;
            zone = z;
        }

        public Missionary(String fN, String n, int i, String z, Leg[] l, String[][] c, MissionaryManager mm)
        {
            fullName = fN;
            name = n;
            id = i;
            legs = l; // (Leg[])l.Clone();
            comps = c;
            //legs = null;
            legIndexs = null;
            manager = mm;
            zone = z;
        }
        

    }

    public class MissionaryManager
    {
        public int preferredStart;
        public int preferredFinish;
        public int minLayover;
        public int timeCostRatio; // How much does a minute of a missionaries time cost?
        public int preferredHubTime;
        public int preferredSleepoverTime;

        public TrainManager trainStation;
        String[] hubs;
        public List<Missionary> missionaries;

        public bool addMissionary(Missionary m)
        {
            try
            {
                m.manager = this;
                missionaries.Add(m);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }
        public bool addMissionary(String fN, String n, int i, String z, Leg[] l, String[][] c)
        {
            try
            {
                missionaries.Add(new Missionary(fN, n, i, z, l, c, this));
            }
            catch (Exception)
            {
                return false;
            }
           return true;
        }

        public bool checkDuplicates()
        {
            List<String> usedNames = new List<String>();
            List<String> duplicates = new List<String>();
            foreach (Missionary m in missionaries){
                if (usedNames.Contains(m.name) && !duplicates.Contains(m.name)){
                    duplicates.Add(m.name);
                } else if (!usedNames.Contains(m.name)){
                    usedNames.Add(m.name);
                }
            }

            foreach (String n in duplicates){
                foreach (Missionary m in missionaries)
                {
                    if (duplicates.Contains(m.name))
                    {
                        // Prompt for what we use...
                        String response = FUNCS.ShowPromptBox("Name Resolution", String.Format("What missionary do you mean by {0} ({1})? Enter his name as you use it in the travel plans (it's in the parenthisis).", m.name, m.fullName), "");
                        m.name = response; // TODO implement a deeper check.                        
                    }
                }
            }
            if (duplicates.ToArray().Length == 0)
            {
                return true;
            }
            else
            {
                Console.WriteLine(String.Format("We still have {0} duplicates.", duplicates.ToArray().Length));
                return checkDuplicates();
            }

        }


        public Missionary setName(int ID, String newName){
            foreach (Missionary m in missionaries)
            {
                if (m.id == ID)
                {
                    m.name = newName;
                    return m;
                }
            }
            return null;
        }
        public Missionary setFullName(int ID, String newName)
        {
            foreach (Missionary m in missionaries)
            {
                if (m.id == ID)
                {
                    m.fullName = newName;
                    return m;
                }
            }
            return null;
        }
       
        public Missionary getMissionaryByID(int ID)
        {
            foreach (Missionary m in missionaries)
            {
                if (m.id == ID)
                {
                    return m;
                }
            }
            return null;
        }

        public Missionary getMissionaryByName(string name)
        {
            foreach (Missionary m in missionaries)
            {
                if (m.name == name)
                {
                    return m;
                }
            }
            return null;
        }

        public void printAllMissionaries(){
            Console.WriteLine("Printing all my missionaries:");
            foreach (Missionary missionary in missionaries)
            {
                Console.WriteLine(missionary.ToString());
            }
            Console.WriteLine(missionaries.ToArray().Length);
        }

        public void printAllMissionarySchedules()
        {
            Console.WriteLine("Printing all my missionaries' schedules:");
            foreach (Missionary missionary in missionaries)
            {
                Console.WriteLine("");
                Console.WriteLine(missionary.fullName + " - " + FUNCS.minutesToTimeString(missionary.getTravelTime()));
                Console.WriteLine("----");
                Console.WriteLine(missionary.getSchedule());

                // Write to test file:
            }
            Analyzation anal = this.analyzeSolution();
            Console.WriteLine(String.Format("Number of conflicts: {0}. \r\n Total cost: {1}. \r\n Total travel time: {2}.", anal.conflictCount, anal.cost, FUNCS.minutesToTimeString(anal.time)));
        }

        public String getAllMissionarySchedules()
        {
            String builder = "Solutions:\r\n\r\n";
            foreach (Missionary missionary in missionaries)
            {

                builder = builder + missionary.fullName + " - " + FUNCS.minutesToTimeString(missionary.getTravelTime());
                builder = builder + "\r\n----\r\n";
                builder = builder + missionary.getSchedule();
                builder = builder + "\r\n\r\n";

            }

            return builder;

        }


        public void calculateTrips()
        {
            foreach (Missionary m in missionaries)
            {
                Console.WriteLine("Filling for: " + m.name);
                String[] stops = m.getStops();
                String[][] comps = m.comps;
                List<String> theirHubs = new List<String>();
                int startStop = 0; // default behavior is start at the beginning of the trip.
                int hubImp = -1;
                int hubInd = -1;

                /****
                 * OK, scriving thoughts. What if I create a MissionaryManager function called getMostDistantComp...
                 * because the person who has to go through 4 stations to meet me at my fourth station should be the most difficult
                 * to match. Thus, we should start there, together....
                 */


                // Check if they have hubs...
                for (int i = 0; i < stops.Length; i++)
                {
                    for (int j = hubs.Length-1; j >= 0; j--)
                    {
                        if (hubs[j].Equals(stops[i]))
                        {
                            hubImp = j;
                            hubInd = i;
                            theirHubs.Add(stops[i]);
                            startStop = i;
                        }
                    }

                }
                // TODO get this to work correctly.
                startStop = m.legs.Length;


                #region hubBackwards
                // We want to start at the most important hub/the default StartStop (0) and work outwards...
                // first backwards...
                for (int i = startStop; i > 0; i--)
                {
                    if (m.legs[i-1].train == null)
                    {
                        String arrive = stops[i];
                        String depart = stops[i - 1];
                        int arriveTime;

                        if (i == hubInd)
                        { // We're at the priority hub...
                            arriveTime = preferredHubTime - minLayover; // plus buffer time because later we'll get rid of it.
                        }
                        else
                        {
                            arriveTime = m.getTimeToEndLeg(arrive) - minLayover; // What time do I need to end this leg?
                        }

                        Train t = trainStation.getTrainToArriveAt(depart, arrive, arriveTime, m.legs[i-1].date);
                        /*
                        while (t.timeMinutes < arriveTime - minLayover)
                        {
                            t = trainStation.getPreviousTrain(t);
                        }*/
                        m.fillLeg(t); 
                    }
                }

                #endregion hubBackwards

                #region beginningForward

                for (int i = startStop; i < stops.Length-1; i++)
                {
                    String arrive = stops[i + 1];
                    String depart = stops[i];
                    int readyTime;

                    // If we are starting at the beginning - leave as early as possible and start packing the travel tight!
                    // UNLESS we have a sleep over the first leg. In that case, we need to plan to get there as close to preferredSleepoverTime as possible.
                    // We're only going to go through this thing once.
                    if (m.legs[i].going_to_sleepover)
                    {
                        // We want to arrive here at bedtime. If this is not element 0, then this SHOULD be handled in the hubBackwards region.
                        // Since we are specifying an ARRIVAL time instead of a DEPARTURE time as usual, we need to handle this separately.

                        Train t = trainStation.getTrainToArriveAt(depart, arrive, preferredSleepoverTime, m.legs[i].date, true);
                        m.fillLeg(t);

                    }
                    else // We're in the normal case scenario.
                    {
                        if (i == 0)
                        {
                            readyTime = preferredStart;
                        }
                        else
                        {
                            readyTime = m.getReadyTimeAt(depart);
                        }
                        Train t = trainStation.getTrainToLeaveAt(depart, arrive, readyTime, m.legs[i].date);
                        m.fillLeg(t);
                    }

                }

                #endregion beginningForward


                Console.WriteLine("Done filling for: " + m.name);
            } 

            
            // Optimize!
            Console.WriteLine("Conflicts: "+this.analyzeSolution().conflictCount.ToString());
            SolutionOptimizer optimizer = new SolutionOptimizer(this.Clone());
            optimizer.optimize(1);
            this.missionaries = optimizer.getSolution().missionaries;
             

        }

        public Analyzation analyzeSolution()
        {
            Analyzation a = new Analyzation();
            a.cost = 0;
            a.time = 0;
            a.conflictCount = 0;
            a.max_arrive = 0;
            a.min_depart = 24*60; // can't get any higher than 24 hours.
            a.null_trains = 0;
            int unalignedComps = 0;

            foreach (Missionary m in missionaries)
            {
                foreach (Leg l in m.legs)
                {
                    if (l.train != null)
                    {
                        if (l.train.costEuro > 0)
                        {
                            a.cost += l.train.costEuro;
                        }
                        a.time += l.train.durationMinutes;
                        if (l.conflict_after == true)
                        {
                            a.conflictCount += 1;
                        }
                        if (l.time_end > a.max_arrive)
                        {
                            a.max_arrive = l.time_end;
                        }
                        if (l.time_start < a.min_depart)
                        {
                            a.min_depart = l.time_start;
                        }
                    }
                    else
                    {
                        a.null_trains++;
                    }
                }
            }
            return a;
        } 

        public MissionaryManager Clone()
        {
            MissionaryManager mm = new MissionaryManager(missionaries, trainStation);
            mm.hubs = this.hubs;
            mm.minLayover = this.minLayover;
            mm.preferredFinish = this.preferredFinish;
            mm.preferredHubTime = this.preferredHubTime;
            mm.preferredStart = this.preferredStart;
            mm.timeCostRatio = this.timeCostRatio;
            mm.trainStation = this.trainStation;
            return mm;


            return null;
        }

        public MissionaryManager (TrainManager t){
            missionaries = new List<Missionary>();
            trainStation = t;
            Setup();
        }
        public MissionaryManager(List<Missionary> m, TrainManager t){
            missionaries = m;
            trainStation = t;
            Setup();
        }

        private void Setup(){
            hubs = trainStation.getHubs();
            minLayover = Properties.Settings.Default.minLayover;
            preferredFinish = Properties.Settings.Default.preferredFinish;
            preferredStart = Properties.Settings.Default.preferredStart;
            preferredHubTime = Properties.Settings.Default.preferredHubTime;
            timeCostRatio = Properties.Settings.Default.timeCostRatio;
            preferredSleepoverTime = Properties.Settings.Default.preferredSleepoverTime;
        }

    }



    public class ExcelManager
    {
        private String date;
        private MissionaryManager mm;
        private TrainManager tm;

        public Leg[] getLegs(String t)
        {
            Regex getLegsChunck = new Regex(@"\{?[\w.'][ \-'.\w]+\}?[ ]*(\[[\w\d]+\:[\w\d]+\])?[ ]*(?:\(['.\w ,\-]+\))?");

            // Uses the same Regex sub-strings to match the different parts in each match.
            Regex getCity = new Regex(@"{?[\w.'][ \-'.\w]+}?");
            Regex getTime = new Regex(@"(\[[\w\d]+\:[\w\d]+\])");
            Regex getComps = new Regex(@"(\(['.\w ,\-]+\))");

            String[] legsRaw = getLegsChunck.Matches(t).Cast<Match>().Select(m => m.Value).ToArray();

            List<Leg> legs = new List<Leg>();

            //List<String> legsL = new List<String>();
            //List<String[]> compsL = new List<String[]>();

            String travelDate = date;

            for (int z = 0; z < legsRaw.Length-1; z++)
            {
                string leg = legsRaw[z];
                string nextLeg = legsRaw[z+1];
                Leg cLeg;

                String city;
                bool sleepover = false;
                int arrival_deadline;
                String[] comps;
                
                // Retrieve info.

                city = getCity.Match(leg).ToString();

                // check for curly braces, so we know to set the sleepover flag, and removes them if present.
                if (city.IndexOf("{") >= 0)
                {
                    // We have them, we need to set the flag.
                    sleepover = true;

                    // Remove the braces from the city name.
                    city = city.Substring(1, city.Length - 2);
                    Console.WriteLine("We stripped a city down to:");
                }

                String time_temp = getTime.Match(leg).ToString();
                if (time_temp.Equals(""))
                {
                    arrival_deadline = -1;
                }
                else
                {
                    arrival_deadline = FUNCS.timeStringToMinutes(time_temp.Substring(1, time_temp.Length - 2));
                }


                String comps_temp = getComps.Match(leg).ToString();
                if (comps_temp.Equals(""))
                {
                    comps = new String[0];
                } 
                else
                {
                    comps = comps_temp.Substring(1, comps_temp.Length - 2).Split(new char[] { ',' });
                    for (int i = 0; i < comps.Length; i++)
                    {
                        comps[i] = comps[i].Trim();
                    }
                }

                // if we're sleeping over at this leg... that means we need to move all of our previous dates backwards one!
                // We also set the going_to_sleepover flag, so that the optimizer knows that we need to travel at night.
                // (Not real effective, but it'll do.)
                if (sleepover)
                {
                    for (int b = 0; b < z; b++)
                    {
                        legs[b].date = FUNCS.addDaysToDateString(legs[b].date, -1);
                        legs[b].going_to_sleepover = true;
                        
                    }

                    // From back when I thought I should just it forward and keep going... except then you're LATE for things, duh! :D
                    //travelDate = FUNCS.addDaysToDateString(travelDate, 1);

                }

                // Create the leg and push it to the list.
                String validStation = tm.validateStation(city);
                cLeg = new Leg(travelDate, comps, validStation, "", false); // leave the arrival/sleepover blank/false, we'll add it when we read it.
                legs.Add(cLeg);
                if (z > 0){
                    legs[z-1].arrive = validStation;
                    legs[z-1].going_to_sleepover = sleepover;
                    legs[z - 1].comps = comps;
                }                
            }

            // we still need to look at the last thing.
            String lastCity = getCity.Match(legsRaw[legsRaw.Length - 1]).ToString();
            bool lastSleepover = false;
            
            // check for curly braces, so we know to set the sleepover flag, and removes them if present.
            if (lastCity.IndexOf("{") >= 0)
            {
                // We have them, we need to set the flag.
                lastSleepover = true;

                // Remove the braces from the city name.
                lastCity = lastCity.Substring(1, lastCity.Length - 2);
                Console.WriteLine("We stripped a city down to:");
            }

            String comps_temp2 = getComps.Match(legsRaw[legsRaw.Length -1]).ToString();
            String[] comps2;
            if (comps_temp2.Equals(""))
            {
                comps2 = new String[0];
            }
            else
            {
                comps2 = comps_temp2.Substring(1, comps_temp2.Length - 2).Split(new char[] { ',' });
                for (int i = 0; i < comps2.Length; i++)
                {
                    comps2[i] = comps2[i].Trim();
                }
            }
                


            // add what we just found.
            legs[legs.Count - 1].going_to_sleepover = lastSleepover;
            legs[legs.Count - 1].arrive = tm.validateStation(lastCity.Trim());
            legs[legs.Count - 1].comps = comps2;
            
            return legs.ToArray();
        }

        public String[][][] getLegsComps(String t)
        {
            //Regex getLegs = new Regex(@"[A-Za-z][ \-A-Za-z]+(?:\([A-Za-z ,-]+\))?"); // Will grab every step
            //Regex getLegs = new Regex(@"[\w.'][ \-'.\w]+(?:\(['.\w ,-]+\))?");
            Regex getLegs = new Regex(@"\{?[\w.'][ \-'.\w]+\}?[ ]*(\[[\w\d]+\:[\w\d]+\])?[ ]*(?:\(['.\w ,-]+\))?");

            // Uses the same Regex sub-strings to match the different parts in each match.
            Regex getCity = new Regex(@"{?[\w.'][ \-'.\w]+}?");
            Regex getTime = new Regex(@"(\[[\w\d]+\:[\w\d]+\])?");
            Regex getComps = new Regex(@"(?:\(['.\w ,-]+\))?");
            
            String[] legsRaw = getLegs.Matches(t).Cast<Match>().Select(m => m.Value).ToArray();

            List<String> legsL = new List<String>();
            List<String[]> compsL = new List<String[]>();

            String city;
            bool sleepover = false;

            foreach (string leg in legsRaw)
            {
                // make sure our sleepover flag is false.
                sleepover = false;



                city = getCity.Match(leg).ToString();

                // check for curly braces, so we know to set the sleepover flag, and removes them if present.
                if (city.IndexOf("{") >= 0)
                {
                    // We have them, we need to set the flag.
                    sleepover = true;

                    // Remove the braces from the city name.
                    city = city.Substring(1, city.Length - 2);
                    Console.WriteLine("We stripped a city down to:");
                }


                /*
                int startComps = leg.IndexOf("(");
                if (startComps == -1) // if there are no comps, we're either goin' solo or we're at our first city - either way, we don't need to think.
                {
                    legsL.Add(tm.validateStation(leg)); // Add the whole string - it's just the city name.
                    compsL.Add(new String[0]);
                }
                else
                {
                    string cityPart = leg.Substring(0, startComps).Trim();
                    string compPart = leg.Substring(startComps + 1, leg.Length - startComps - 2).Trim();
                    string[] comps = compPart.Split(new char[] { ',' });
                    for (int i = 0; i < comps.Length; i++)
                    {
                        comps[i] = comps[i].Trim();
                    }
                    legsL.Add(tm.validateStation(cityPart));
                    compsL.Add(comps);
                } */
            }

            string[][] returnLegs = new string[][] { legsL.ToArray() };
            string[][] returnComps = compsL.ToArray();

            return new string[][][] { returnLegs, returnComps };
            // return new string[][] { new string[] {legsL.ToArray() }, compsL.ToArray() };
        }
        
        public void Import_Travel_File(String fileURI, String sheetName)
        {


            var connectionString = string.Format("Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties=Excel 12.0;", fileURI);

            var adapter = new OleDbDataAdapter();
            try
            {
                adapter = new OleDbDataAdapter("SELECT * FROM [" + sheetName + "$]", connectionString);
            }
            catch (Exception e)
            {
                adapter = new OleDbDataAdapter("SELECT * FROM [mini$]", connectionString);
            }
            var ds = new DataSet();

            adapter.Fill(ds, "mini");

            DataTable data = ds.Tables["mini"];
            DataRowCollection rows = data.Rows;
            DataColumnCollection columns = data.Columns;

            int numberOfRows = data.Rows.Count;

            int blankRowCount = 0;
            bool rowBlank;

            int nextMissionaryID = 0;

            for (int i = 0; i < numberOfRows; i++)
            {
                DataRow currentRow = rows[i];

                /* we want to check to see if there's content here, so we know when the table ends. */
                rowBlank = true; // we begin assuming that a row is blank.
                for (int j = 1; j < currentRow.ItemArray.Length; j++)
                {
                    // Check to see if the cell has content. If so, make rowBlank false.
                    if (!(currentRow[j].ToString().Equals("")))
                    {
                        rowBlank = rowBlank & false;
                        blankRowCount = 0; // We only want to count consecutive rows, so reset to zero, we found content.
                        break; // we don't need to keep looking for content, we found some, so it's not blank.
                    }
                    //Console.Write(rows[i][j]);
                    //Console.Write(" - ");
                }

                if (rowBlank)
                {
                    blankRowCount += 1;
                    if (blankRowCount > 3)
                    {
                        break; // We quit, we don't think there's anymore content.
                    }
                }
                else // There's content here, let's check it and add it.
                {
                    #region parseRow

                    //Console.WriteLine(currentRow.ToString());
                    String cFullName = currentRow[data.Columns.IndexOf("Name")].ToString().Trim();
                    String cUsedName = currentRow[data.Columns.IndexOf("Used Name")].ToString().Trim();

                    String home = currentRow[data.Columns.IndexOf("Home")].ToString();
                    String travelTo = currentRow[data.Columns.IndexOf("Travel To (With)")].ToString();
                    String waitsIn = currentRow[data.Columns.IndexOf("Waits in City (With)")].ToString();
                    String returnWith = currentRow[data.Columns.IndexOf("Return With")].ToString();
                    String travelToCity = currentRow[data.Columns.IndexOf("Travel to New City (With)")].ToString();
                    String zone = currentRow[data.Columns.IndexOf("Zone")].ToString();

                    bool noChange = currentRow[data.Columns.IndexOf("No Change")].ToString().Equals("x");

                    // If "home"turns out to be a coppia name, we need to figure out the city.
                    
                    Regex shortWord = new Regex(@"((?!Anz$)(?!Sor$)(?=[^\d])(\w{2,} ?))");
                    String[] homeWords = shortWord.Matches(home).Cast<Match>().Select(m => m.Value).ToArray();
                    String homeBuilder = "";
                    foreach (String s in homeWords){
                        if (!(s.Equals("nz") || s.Equals("or"))){
                            homeBuilder = homeBuilder + s;
                        }
                        
                    }
                    Console.WriteLine("\r\n\r\nWe changed home '"+home+"' to '"+homeBuilder);
                     
                    home = homeBuilder.Trim();

                    #endregion

                    #region legsCompsSwitches

                    String legsCompsString = "";
                    if (home.Length > 0)
                    {
                        legsCompsString = home;
                    }

                    if (travelTo.Length > 0)
                    {
                        if (legsCompsString.Length > 1)
                        {
                            legsCompsString = legsCompsString + ", " + travelTo;
                        }
                        else
                        {
                            legsCompsString = travelTo;
                        }
                    }

                    if (waitsIn.Length > 0)
                    {
                        if (legsCompsString.Length > 1)
                        {
                            legsCompsString = legsCompsString + ", " + waitsIn;
                        }
                        else
                        {
                            legsCompsString = waitsIn;
                        }
                    }

                    if (returnWith.Length > 0)
                    {
                        if (legsCompsString.Length > 1)
                        {
                            legsCompsString = legsCompsString + ", " + returnWith;
                        }
                        else
                        {
                            legsCompsString = returnWith;
                        }
                    }

                    if (travelToCity.Length > 0)
                    {
                        if (legsCompsString.Length > 1)
                        {
                            legsCompsString = legsCompsString + ", " + travelToCity;
                        }
                        else
                        {
                            legsCompsString = travelToCity;
                        }
                    }

                    #endregion

                    if (legsCompsString.Length > home.Length)
                    {
                        Leg[] legs = getLegs(legsCompsString);

                        // generate the rather repetitive but ormai too engrained to care comps...
                        String[][] comps;
                        List<String[]> comps_temp = new List<String[]>();
                        foreach (Leg l in legs){
                            comps_temp.Add(l.comps);
                        }
                        comps = comps_temp.ToArray();
                        
                       
                        


                        if ((cFullName.Length > 0) & (cUsedName.Length > 0))
                        {
                            mm.addMissionary(cFullName, cUsedName, nextMissionaryID++, zone, legs, comps);
                            
                        }
                        else
                        {
                            //Console.WriteLine("An error occurred with "+cMissionary.ToString());
                        }
                    }
                    else
                    {
                        // I'm not gonna worry about you if you don't move and do nothing.
                    }



                }
                mm.checkDuplicates();
            }
        }

        public bool write_to_text_file(String fileURI, String fileName, String content)
        {
            while (!fileURI.Substring(fileURI.Length-3,2).Equals("\\\\")){
                fileURI = fileURI + "\\";
                Console.WriteLine("Now fileURI is: "+fileURI);
            }
            System.IO.File.WriteAllText(fileURI+fileName, content);

            return false;
        }

        public TrainManager getConductor()
        {
            return tm;
        }

        public MissionaryManager getMissionaryManager()
        {
            return mm;
        }
    
        public ExcelManager(String d){
            date = d;
            tm = new TrainManager(date);
            mm = new MissionaryManager(tm);
        }
    
    
    
    }
    
    public class SolutionOptimizer
    {
        MissionaryManager manager { get; set; }
        Missionary[] currentSolution;

        public void optimize(int depth)
        {

            Analyzation before = manager.analyzeSolution();

            MissionaryManager resultMM = independentOptimizerRandomOrder(manager);
            
            Analyzation after = resultMM.analyzeSolution();
            Console.WriteLine(after.conflictCount);
            Console.WriteLine("---");
            Console.WriteLine(before.conflictCount.ToString() + " -> " + after.conflictCount.ToString() + " conflicts.");
            Console.WriteLine(before.cost.ToString() + " -> " + after.cost.ToString() + " cost.");
            Console.WriteLine(before.time.ToString() + " -> " + after.time.ToString() + " total time.");
            Console.WriteLine(before.null_trains.ToString() + " -> " + after.null_trains.ToString() + " null trains.");
            Console.WriteLine("---");
            while (!after.Equals(before))
            {
                manager = resultMM;
                before = manager.analyzeSolution();
                resultMM = independentOptimizerRandomOrder(manager);
                after = manager.analyzeSolution();
                Console.WriteLine("---");
                Console.WriteLine(before.conflictCount.ToString() + " -> " + after.conflictCount.ToString());
                Console.WriteLine(before.cost.ToString() + " -> " + after.cost.ToString());
                Console.WriteLine(before.time.ToString() + " -> " + after.time.ToString());
                Console.WriteLine("---");
                Console.WriteLine(after.conflictCount);
                resultMM = independentOptimizerRandomOrder(manager);
            }
            Console.WriteLine("We got done! There are "+after.conflictCount.ToString()+" conflicts.");
        }

        public MissionaryManager getSolution()
        {
            return manager;
        }

        public MissionaryManager independentOptimizerRandomOrder(MissionaryManager inputMM)
        {
            MissionaryManager mm = inputMM.Clone();
            Random rand = new Random();
            List<Missionary> ms = mm.missionaries;
            int numberOfMissionaries = ms.Count;
            List<int> missionaryIndices = new List<int>();
            for (int i = 0; i < numberOfMissionaries; i++)
            {
                missionaryIndices.Add(i);
            }
            
            // until we've done every missionary...
            while (missionaryIndices.Count > 0)
            {
                int randomInd = rand.Next(0,missionaryIndices.Count);
                int missInd = missionaryIndices[randomInd];
                Missionary currentMissionary = ms[missInd];
                missionaryIndices.Remove(missInd);

                // If we're already getting up early enough, start at the end (or, if we're going to a sleepover, which means we need to always squish forward.)
                if (currentMissionary.legs[0].time_start < 9 * 60 || currentMissionary.legs[0].going_to_sleepover)
                {
                    // For each train...
                    for (int i = currentMissionary.legs.Length - 1; i >= 0; i--)
                    {
                        // As long as it conflicts with that before it, move it forward.
                        while (currentMissionary.legs[i].conflict_before == true)
                        {
                            Train replacement = manager.trainStation.getNextTrain(currentMissionary.legs[i].train);
                            /* The fillLeg should do this...
                             if (i > 0 && replacement.timeMinutes > currentMissionary.legs[i - 1].train.arrivalTimeMinutes)
                            {
                                currentMissionary.legs[i].conflict_before = false;
                                currentMissionary.legs[i - 1].conflict_after = false;
                            }
                            else
                            {
                                currentMissionary.legs[i].conflict_before = true;
                                currentMissionary.legs[i - 1].conflict_after = true;
                            }

                            if (i < currentMissionary.legs.Length - 1 && replacement.arrivalTimeMinutes > currentMissionary.legs[i + 1].train.timeMinutes)
                            {
                                currentMissionary.legs[i].conflict_after = true;
                                currentMissionary.legs[i+1].conflict_before = true;
                            }
                            else
                            {
                                currentMissionary.legs[i].conflict_after = false;
                                currentMissionary.legs[i+1].conflict_before = false;
                            } */
                            //currentMissionary.legs[i].train = replacement;
                            currentMissionary.fillLeg(replacement);
                        }
                    }
                }
                else
                {
                    // For each train...
                    for (int i = 0; i < currentMissionary.legs.Length-1; i++)
                    {
                        // As long as it conflicts with that after it, move it back.
                        while (currentMissionary.legs[i].conflict_after == true)
                        {
                            Train replacement = manager.trainStation.getPreviousTrain(currentMissionary.legs[i].train);
                            /* The fillLeg should do this
                            if (i > 0 && replacement.timeMinutes > currentMissionary.legs[i - 1].train.arrivalTimeMinutes)
                            {
                                currentMissionary.legs[i].conflict_before = false;
                                currentMissionary.legs[i - 1].conflict_after = false;
                            }
                            else
                            {
                                if (i > 0)
                                {
                                    currentMissionary.legs[i].conflict_before = true;
                                    currentMissionary.legs[i - 1].conflict_after = true;
                                }
                            }

                            if (i < currentMissionary.legs.Length - 1 && replacement.arrivalTimeMinutes > currentMissionary.legs[i + 1].train.timeMinutes)
                            {
                                currentMissionary.legs[i].conflict_after = true;
                                currentMissionary.legs[i+1].conflict_before = true;
                            }
                            else
                            {
                                if (i < currentMissionary.legs.Length)
                                {
                                    currentMissionary.legs[i].conflict_after = false;
                                    currentMissionary.legs[i + 1].conflict_before = false;
                                }
                            } */
                            //currentMissionary.legs[i].train = replacement;
                            currentMissionary.fillLeg(replacement);
                        }
                    }
                }

                // We've done the whole missionaries travel now.
                ms[missInd] = currentMissionary; // put it back.
            }
            // We've done all the missinaries.

            // Compare it to that with which we started, and if it's better, return it, otherwise the original.
            mm.missionaries = ms;
            Analyzation inputA = inputMM.analyzeSolution();
            Analyzation outputA = mm.analyzeSolution();

            if (compareAnalyzations(inputA, outputA) == 0)
            {
                return inputMM;
            }
            else
            {
                return mm;
            }

            return mm;

        }

        // Returns 0 if a is better, 1 if b is better.
        public int compareAnalyzations(Analyzation a, Analyzation b)
        {
            if (a.conflictCount < b.conflictCount)
            {
                return 0;
            } if (a.conflictCount > b.conflictCount)
            {
                return 1;
            }
            else
            {
                // TODO... make it better. For now, return the one with the lowest time.
                if (a.time <= b.time)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
        }

        public SolutionOptimizer(MissionaryManager m)
        {
            manager = m;
            //currentSolution = m..;
        }


    }

    static class TRAIN_TYPES
    {
        public const int FRECCIA_BIANCA = 0;
        public const int FRECCIA_ROSSA = 1; 
        public const int FRECCIA_ARGENTA = 2;
        public const int REGIONALE = 3;
        public const int REGIONALE_VELOCE = 4;

        public static readonly Dictionary<String, int> codes = new Dictionary<String, int> { { "Frecciabianca", FRECCIA_BIANCA }, { "Frecciarossa", FRECCIA_ROSSA }, { "Frecciaargente", FRECCIA_ARGENTA }, { "Regionale", REGIONALE }, { "Regionale Veloce", REGIONALE_VELOCE } };

        public static string getTypeFromNumber(int i)
        {
            switch (i)
            {
                case 0:
                    return "Frecciabianca";
                    break;
                case 1:
                    return "Frecciarossa";
                    break;
                case 2:
                    return "Frecciaargento";
                    break;
                case 3:
                    return "Regionale";
                    break;
                case 4:
                    return "Regionale Veloce";
                    break;
                default:
                    return "Unkown train type";

            }
        }

    }

    static class ERROR
    {
        public const int INVALID_STATION = 0;
    }

    public class Leg
    {

        public Train train { get; set; }
        public String[] comps { get; set; }
        public String depart { get; set; }
        public String arrive { get; set; }
        public String date { get; set; }
        public int time_start { get; set; }
        public int time_end { get; set; }
        public bool conflict_before { get; set; }
        public bool conflict_after { get; set; }
        public bool going_to_sleepover { get; set; }
        public int arrival_deadline { get; set; }
        public bool skip { get; set; }

        public Leg(String[] c, String d, String a)
        {
            comps = c;
            depart = d;
            arrive = a;
            conflict_after = false;
            conflict_before = false;
            skip = false;
        }

        public Leg(String dt, String[] cmps, String dep, String arr, bool slpvr)
        {
            date = dt;
            comps = cmps;
            depart = dep;
            arrive = arr;
            going_to_sleepover = slpvr;
            conflict_after = false;
            conflict_before = false;
            skip = false;
        }

        /*public Leg(String[] c, int ts, int te)
        {
            comps = c;
            time_start = ts;
            time_end = te;
        }*/
        public Leg(String[] c, int ts, int te, bool cb, bool ca, bool slpvr)
        {
            comps = c;
            time_start = ts;
            time_end = te;
            going_to_sleepover = slpvr;
            conflict_before = cb;
            conflict_after = ca;
            skip = false;
        }

    }

    // hashtag why am I using c#?
    public static class FUNCS // Just holds useful functions I want to use all over, because I don't know how c# should work this. :)
    {

        public static String addDaysToDateString(String dateString, int days)
        {
            try
            {
                String[] parts = dateString.Split(new char[] { '-' });
                DateTime changedDate = new DateTime(Int32.Parse(parts[2]), Int32.Parse(parts[1]), Int32.Parse(parts[0])).AddDays(days);
                Console.WriteLine();
                return String.Format("{0}-{1}-{2}", twoDigitHour(changedDate.Day.ToString()), twoDigitHour(changedDate.Month.ToString()), changedDate.Year.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Invalidly formatted string fed to addDaysToDateString: " + dateString);
                return dateString;
            }
        }

        public static String ShowPromptBox(String title, String question, String defaultText)
        {
            Form3 testDialog = new Form3();
            testDialog.textBox1.Text = defaultText;
            testDialog.textBox2.Text = question;
            testDialog.Text = title;
            String txtResult;
            // Show testDialog as a modal dialog and determine if DialogResult = OK.
            if (testDialog.ShowDialog() == DialogResult.OK)
            {
                // Read the contents of testDialog's TextBox.
                txtResult = testDialog.textBox1.Text;
            }
            else
            {
                txtResult = "Cancelled";
            }
            testDialog.Dispose();
            return txtResult;
        }

        // Returns the int defined in class TRAIN_TYPES for given string, or returns -1.
        public static int getTrainTypeCode(String s)
        {
            int code = -1;
            try
            {
                code = TRAIN_TYPES.codes[s];
            }
            catch (Exception)
            {
                return -1;
            }
            return code;
        }

        // Returns the hour part of the current time expressed in minutes from midnight.
        public static String twoDigitHour(int minutes)
        {
            return Math.Floor((double) minutes / (double) 60).ToString();
        }

        public static String twoDigitHour(String hour)
        {
            if (hour.Length == 1)
            {
                return "0" + hour;
            }
            else
            {
                return hour;
            }
        }
        
        public static String minutesToTimeString(int m)
        {
            decimal hour = decimal.Floor(m / 60);
            int minutes = m % 60;
            String time = hour.ToString() + ":" + FUNCS.twoDigitHour(minutes.ToString());
            return time;
        }

        public static int timeStringToMinutes(String t)
        {
            string[] parts = t.Split(new Char[] { ':' });
            if (parts[0].Length > 2)
            {
                parts[0] = parts[0].Substring(parts[0].Length - 2, 2);
            }
            if (parts[1].Length > 2)
            {
                parts[1] = parts[1].Substring(0,2);
            }
            return ((Int32.Parse(parts[0]) * 60) + (Int32.Parse(parts[1])));
        }


     }

    public struct Analyzation
    {
        public int cost;
        public int time;
        public int conflictCount;
        public int max_arrive;
        public int min_depart;
        public int null_trains;
    }
        
}

/* 
 * 
 *    //

 * 
  Left over junk I don't want to lose if I need it later, but will delete when we've got a working copy going.
 * 
 * 
  void document_loaded(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            var send = sender as WebKit.WebKitBrowser;
            if (send.Url == e.Url && send.Url.ToString() == "http://www.trenitalia.com/")
            {
                Console.WriteLine("We're in!");
                webKitBrowser1.Document.GetElementById("biglietti_fromNew").SetAttribute("value", "Milano Centrale");
                webKitBrowser1.Document.GetElementById("biglietti_fromNew").NodeValue = "Milano Centrale";
                webKitBrowser1.Document.GetElementById("biglietti_toNew").NodeValue = "Torino Porta Susa";
                //send.Document.GetElementsByTagName("button").ElementAt(3);
                webKitBrowser1.StringByEvaluatingJavaScriptFromString("alert('submitting')");
                webKitBrowser1.StringByEvaluatingJavaScriptFromString("document.getElementsByTagName('button')[3].click()");
                Console.WriteLine("should be done");
            }
            else
            {
                Console.WriteLine("different the second time: "+send.Url.ToString());
            }
        }
 
 * 
 * (Was inside Analyze File)
 * 
 * 
            
            //Console.WriteLine(minutesToTimeString(timeStringToMinutes("10:30")));
            //string[][] kcb = getTrainTimes("Milano Centrale", "Torino Porta Susa", "09-06-2015", "06");
            //Console.WriteLine(kcb);
            /* WebClient client = new WebClient();
            Stream data = client.OpenRead("https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it");
            StreamReader reader = new StreamReader(data);
            string url = reader.ReadToEnd();
            //Console.WriteLine(s);
            data.Close();
            reader.Close();

            
            var solutions = root.Descendants("tr").Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("solutionRow")).ToList(); Where(d => d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("time")).ToList();
            Console.WriteLine("-");
            int quanti = 0;
            foreach (var tr in solutions)
            {
                //Console.WriteLine(tr.InnerHtml);
                quanti++;
            }
            Console.WriteLine(solutions[0].InnerHtml);
            Console.WriteLine("e abbiamo..." + quanti.ToString());
            //System.IO.StreamWriter file = new System.IO.StreamWriter("C:\\Users\\SEC2016583\\Desktop\\treni2.html");
            //file.WriteLine(html);
            //file.Close();
            
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            

            //Solution: WebKit browser
            webKitBrowser1.Navigate("www.trenitalia.com");
            //webKitBrowser1.
            
            webKitBrowser1.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(document_loaded);
         
 * 
 * * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
        public String[][] getTrainTimes(String departure, String arrival, String date, String time)
        {
            // Return object:
            String[][] times = new string[10][]; // Makes space for 10 solutions of 2 times each.

            Dictionary<String, String> postArgs = new Dictionary<string, string>(){
                {"url_desktop", "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it"},
                {"url_mobile", "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it"},
                {"tripType", "on"},
                {"isRoundTrip", "false"},
                {"departureStation", "Milano Centrale"},
                {"arrivalStation", "Torino Porta Susa"},
                {"departureDate", "06-06-2015"},
                {"returnDate", "06-06-2015"},
                {"ynFlexibleDates", "off"},
                {"selectedTrainType", "tutti"},
                {"selectedTrainClassifications", ""}
            };

            postArgs["departureStation"] = departure;
            postArgs["arrivalStation"] =  arrival;
            postArgs["departureDate"] = date;
            postArgs["departureTime"] = twoDigitHour(time);

            string post_data = createPostDataString(postArgs);
            Console.WriteLine(post_data);
            string uri = "https://www.lefrecce.it/B2CWeb/searchExternal.do?parameter=initBaseSearch&amp;lang=it";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";
            //request.AllowAutoRedirect = false;
            request.CookieContainer = new CookieContainer(); // Handles the redirects, etc.

            byte[] postBytes = Encoding.ASCII.GetBytes(post_data);

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;
            Stream requestStream = request.GetRequestStream();

            requestStream.Write(postBytes, 0, postBytes.Length);
            requestStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            String html = new StreamReader(response.GetResponseStream()).ReadToEnd();
            //Console.WriteLine(html);

            HtmlAgilityPack.HtmlDocument htmlDoc = new HtmlAgilityPack.HtmlDocument();
            htmlDoc.LoadHtml(html);

            var root = htmlDoc.DocumentNode;
            int counterSR = 0;
            int counterT = 0;

            foreach (HtmlNode node in root.SelectNodes("//*[contains(@class,'solutionRow')]"))
            {

                String[] currentTime = new string[2];
                
                Console.WriteLine("Solution " + counterSR.ToString());
                foreach (HtmlNode node2 in node.SelectNodes(".//td/div/span[contains(@class, 'time')]"))
                {
                    string thisTime = node2.InnerHtml.Trim();
                    if (counterT % 2 == 0)
                    {
                        Console.WriteLine("Depart: " + thisTime);
                        currentTime[0] = thisTime;
                    }
                    else
                    {
                        Console.WriteLine("Arrive: " + thisTime);
                        currentTime[1] = thisTime;
                    }
                    counterT++;

                }
                times[counterSR] = (String[]) currentTime.Clone();
                counterSR++;
            }
            Console.WriteLine("We got " + counterT.ToString() + " times in " + counterSR.ToString() + " solutions.");
            return times;
        }

 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
*/