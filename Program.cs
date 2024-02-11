using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeastAverageCS
{
    internal class Program
    {
        // DATA ALL FUNCTIONS NEEED:
        static ulong[] AverageArr = new ulong[3840 * 2160 * 3]; // 2160p source images, why? Because most of my stuff is that size. 
                                                                // I don't know if it's possible to set the array size at runtime with the largest file in the set,
                                                                // OR resize everything to 2160p at runtime to avoid getting into problems. 
        static ulong[] MaxEcludianArr = new ulong[3840 * 2160 * 3];
        static int[] EuclideanDistanceFromAvgINTArr = new int[3840 * 2160];

        static int SourceFileWidth = 0;
        static int SourceFileHeight = 0;

        static ulong filenumber = 0;

        static void SetSourceFileWidth(int x)
        { SourceFileWidth = x; }
        static void SetSourceFileHeight(int y)
        { SourceFileHeight = y; }
        static int GetSourceFileWidth()
        {
            return SourceFileWidth;
        }
        static int GetSourceFileHeight()
        {
            return SourceFileHeight;
        }



        // FUNCTIONS USED ON THE DATA
        static ulong[] BitmapFileToArray(string file)
        {
            Bitmap bm = new Bitmap(file);
            Bitmap newBmp = new Bitmap(bm);
            Bitmap targetBmp = newBmp.Clone(new Rectangle(0, 0, newBmp.Width, newBmp.Height), PixelFormat.Format32bppArgb); // Horrible, but it's the only way to get the format we require. Sorry.

            var snoop = new BmpPixelSnoop(targetBmp);
            SetSourceFileWidth(snoop.Width);
            SetSourceFileHeight(snoop.Height);
            ulong pixelnumber = 0; //Actually this is the finalArrayResult element counter, because it's the best way I have found to convert a (x,y) value with 3 z points to a linear array. 

            ulong[] fileArrayResult = new ulong[snoop.Height * snoop.Width * 3]; // length*width*depth

            for (int j = 0; j != snoop.Height; j++)
            {
                for (int i = 0; i != snoop.Width; i++)
                {
                    var col = snoop.GetPixel(i, j);
                    fileArrayResult[pixelnumber] = (ulong)col.R; // current number
                    fileArrayResult[pixelnumber + 1] = (ulong)col.G; // current + 1
                    fileArrayResult[pixelnumber + 2] = (ulong)col.B; // current +2
                    pixelnumber += 3; // which means the next Red pixel should be +3

                }
            }

            return fileArrayResult;
        }

        static void AddArrayToAverageArray(ulong[] newArr)
        {
            for (int i = 0; i < newArr.Length; i++)
            {
                AverageArr[i] = AverageArr[i] + newArr[i];

            }
            filenumber++; // global file number, only incremented here, read in the average function
        }
        static void CalculateAverage()
        {
            for (int i = 0; i < AverageArr.Length; i++)
            {
                AverageArr[i] = AverageArr[i] / filenumber;
            }
        }


        static void FindMaxEuclideanDistance(ulong[] testArr)
        {
            int pixelnumber = 0; // the only number not set per RGB value.
            // test each 3 section of current maximum ecludian distance, if heigher, add the current 3 numbers to MaxEcludianArr. 
            // 1: Find test distance sqrt((AvgArr-testArr)^2) per color. Euclidean distance from https://www.andreweckel.com/LeastAverageImage/
            // 2: Test if distance is heigher than current max with EcludianDistanceFromAvgINTArr[pixelNumber]
            // 3: If distance is heigher, replace max in EcludianDistanceFromAvgINTArr[pixelnumber] and MaxEcludianArr[testArrElement], MaxEcludianArr[testArrElement+1] and MaxEcludianArr[testArrElement+2]
            // 4: if distance is not heigher, text next pixel (eg increase testArrElement by 3

            for (int testArrElement = 0; testArrElement < testArr.Length; testArrElement += 3)
            {
                int distance = (int)Math.Sqrt((testArr[testArrElement] - AverageArr[testArrElement]) ^ 2 + (testArr[testArrElement + 1] - AverageArr[testArrElement + 1]) ^ 2 + (testArr[testArrElement + 2] - AverageArr[testArrElement + 2]) ^ 2);
                if (distance > EuclideanDistanceFromAvgINTArr[pixelnumber])
                {
                    EuclideanDistanceFromAvgINTArr[pixelnumber] = distance;

                    MaxEcludianArr[testArrElement] = testArr[testArrElement];
                    MaxEcludianArr[testArrElement + 1] = testArr[testArrElement + 1];
                    MaxEcludianArr[testArrElement + 2] = testArr[testArrElement + 2];
                }
                pixelnumber++;
            }



        }


        static Bitmap ArrayToBitmap(ulong[] arr)
        {
            Bitmap bm = new Bitmap(GetSourceFileWidth(), GetSourceFileHeight());
            int xVal = 0;
            int yVal = 0;
            int totalPixels = 0;
            using (var snoop = new BmpPixelSnoop(bm))
            {
                for (int a = 0; a < arr.Length; a += 3)
                {
                    Color c = Color.FromArgb((int)arr[a], (int)arr[a + 1], (int)arr[a + 2]); // Get the pixel data from array
                    snoop.SetPixel(xVal, yVal, c); // set the pixel data at x,y
                    xVal++; // Move on to the next x value
                    totalPixels++;
                    //Console.WriteLine("xval = " + xVal + " yval = " + yVal+" total pixels = "+totalPixels);
                    if (xVal == GetSourceFileWidth()) // if we reach the end of x, go to next line
                                                      // the ==1 is because pixeldata starts at (0,0). The .Length starts at 1.
                    {
                        xVal = 0;
                        yVal++;
                    }
                }
            }

            return bm;
        }


        static void Main(string[] args)
        {
            String SaveFilename = "avg.png";
            if (File.Exists(SaveFilename))
            {
                File.Delete(SaveFilename);
            }
            String MaxFilename = "max.png";
            if (File.Exists(MaxFilename))
            {
                File.Delete(MaxFilename);
            }


            List<string> list = new List<string>();

            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.png");
            if(files.Length==0)
            {
                Console.WriteLine("No PNG files in current directory");
                Console.ReadKey();
                return;
            }
            Console.WriteLine("Starting the averiging process");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (string file in files) // Source files for the average array
            {
                AddArrayToAverageArray(BitmapFileToArray(file));
                long ms = stopwatch.ElapsedMilliseconds;
                Console.WriteLine(file + " took " + ms.ToString() + " ms");
                stopwatch.Restart();
            }
            stopwatch.Stop();

            CalculateAverage(); // create the average array

            Bitmap AverageBitmap = ArrayToBitmap(AverageArr);
            
            AverageBitmap.Save(SaveFilename);
            
            stopwatch.Start();
            Console.WriteLine("Starting the max euclidian process");
            foreach (string file in files) // find the max euclidian distance 
            {
                FindMaxEuclideanDistance(BitmapFileToArray(file));
                long ms = stopwatch.ElapsedMilliseconds;
                Console.WriteLine(file + " took " + ms.ToString() + " ms");
                stopwatch.Restart();
            }
            stopwatch.Stop();

            Bitmap bm = ArrayToBitmap(MaxEcludianArr);
            
            bm.Save(MaxFilename);

            /////---------------------------------------------////////
            // Test part of the program that works!

            //ulong[] a = BitmapFileToArray("1.png");
            //AddArrayToAverageArray(array1);
            //CalculateAverage(); // create the average array

            //Bitmap bm = ArrayToBitmap(a);
            //bm.Save("2.png");


            /////---------------------------------------------////////

        }
    }
}