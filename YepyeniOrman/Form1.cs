    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    namespace YepyeniOrman
    {
        public partial class Form1 : Form
        {
            private int[,] graphMatrix;
            private Point[] regionCoordinates;
            private int[] fireIntensity = { 0, 1, 3, 2, 5, 4, 5, 2, 1, 4, 3, 5, 2, 1 };
            private int[] fireTime = { 10, 20, 30, 40, 50 };
            private int[] waterRequired = { 1000, 2000, 3000, 4000, 5000 };
            private int fuelCapacity = 5000;
            private int waterCapacity = 20000;
            private int fuel = 5000;
            private int water = 20000;
            private Timer animationTimer;
            private Queue<Point> movementQueue = new Queue<Point>();
            private Point currentLocation;
            private int totalWaterUsed = 0;      
            private int totalFuelUsed = 0;        
            private int totalDistanceTravelled = 0;
            private int refuelCount = 0;          
            private int waterRefillCount = 0;     
            private List<int> extinguishedFires = new List<int>(); 

            public Form1()
            {
                InitializeComponent();
                LoadGraph();
                pnlMap.Paint += PnlMap_Paint;
                animationTimer = new Timer();
                animationTimer.Interval = 1000; 
                animationTimer.Tick += AnimationTimer_Tick;

            }
        private void PnlMap_Paint(object sender, PaintEventArgs e)
        {
            if (graphMatrix == null) return;

            Graphics g = e.Graphics;
            Pen linePen = new Pen(Color.Gray, 2);

            for (int i = 0; i < graphMatrix.GetLength(0); i++)
            {
                for (int j = 0; j < graphMatrix.GetLength(1); j++)
                {
                    if (graphMatrix[i, j] > 0)
                    {
                        g.DrawLine(linePen, regionCoordinates[i], regionCoordinates[j]);

                        float midX = (regionCoordinates[i].X + regionCoordinates[j].X) / 2;
                        float midY = (regionCoordinates[i].Y + regionCoordinates[j].Y) / 2;
                        g.DrawString(graphMatrix[i, j].ToString(), this.Font, Brushes.Black, midX, midY);
                    }
                }
            }

            for (int i = 0; i < regionCoordinates.Length; i++)
            {
                Brush brush = i == 0 ? Brushes.Blue : Brushes.Red;
                g.FillEllipse(brush, regionCoordinates[i].X - 10, regionCoordinates[i].Y - 10, 20, 20);
                g.DrawString($"B{i}", this.Font, Brushes.Black, regionCoordinates[i].X - 15, regionCoordinates[i].Y - 25);

                if (i != 0)
                {
                    string fireText = fireIntensity[i] > 0 ? $"Yangın: {fireIntensity[i]}" : "Sönmüş";
                    Brush textBrush = fireIntensity[i] > 0 ? Brushes.Black : Brushes.Green;
                    g.DrawString(fireText, this.Font, textBrush, regionCoordinates[i].X - 15, regionCoordinates[i].Y + 15);
                }
            }


            g.FillEllipse(Brushes.Green, currentLocation.X - 10, currentLocation.Y - 10, 20, 20);
        }


        private Point[] CalculateRegionCoordinates(int size)
            {
                Point[] coordinates = new Point[size];

                int panelCenterX = pnlMap.Width / 2;
                int panelCenterY = pnlMap.Height / 2;

                double radius = Math.Min(pnlMap.Width, pnlMap.Height) / 2.5;
                double angleStep = 2 * Math.PI / size;

                for (int i = 0; i < size; i++)
                {
                    double angle = i * angleStep;
                    int x = panelCenterX + (int)(radius * Math.Cos(angle));
                    int y = panelCenterY + (int)(radius * Math.Sin(angle));
                    coordinates[i] = new Point(x, y);
                }

                return coordinates;
            }
            private void LoadGraph()
            {
                try
                {
                    string filePath = "Orman.txt";
                    string[] lines = File.ReadAllLines(filePath);

                    int rowCount = lines.Length;
                    int columnCount = lines[0].Split(',').Length;

                    if (rowCount != columnCount)
                    {
                        throw new Exception("TXT dosyasında satır ve sütun sayısı eşleşmiyor!");
                    }

                    graphMatrix = new int[rowCount, columnCount];

                    for (int i = 0; i < rowCount; i++)
                    {
                        string[] parts = lines[i].Split(',');

                        for (int j = 0; j < columnCount; j++)
                        {
                            graphMatrix[i, j] = int.Parse(parts[j]);
                        }
                    }

                    regionCoordinates = CalculateRegionCoordinates(rowCount);

                    MessageBox.Show("Graf başarıyla yüklendi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    pnlMap.Invalidate();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            private void StartAnimation(List<int> path)
            {
                movementQueue.Clear();
                foreach (int region in path)
                {
                    movementQueue.Enqueue(regionCoordinates[region]);
                }

                if (movementQueue.Count > 0)
                {
                    currentLocation = movementQueue.Dequeue();
                    animationTimer.Start();
                }
            }
        private int CalculateDistance(Point start, Point end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            return (int)Math.Sqrt(dx * dx + dy * dy);
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (movementQueue.Count > 0)
            {
                Point nextLocation = movementQueue.Dequeue();


                int distance = CalculateDistance(currentLocation, nextLocation);


                if (fuel < distance)
                {
                    MessageBox.Show("Yakıt yetersiz! B0 noktasına dönülüyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    ReturnToBase();
                    return;
                }


                fuel -= distance;
                totalDistanceTravelled += distance;
                pnlStatus.Invalidate(); 

                currentLocation = nextLocation;
                pnlMap.Invalidate(); 
            }
            else
            {
                animationTimer.Stop();
                ExtinguishFireAtCurrentLocation(); 
            }
        }

        private (List<int> path, int distance) Dijkstra(int[,] graph, int start, int target)
        {
            int size = graph.GetLength(0);
            bool[] visited = new bool[size];
            int[] distance = new int[size];
            int[] previous = new int[size];


            for (int i = 0; i < size; i++)
            {
                distance[i] = int.MaxValue;
                previous[i] = -1;
            }

            distance[start] = 0;

            for (int i = 0; i < size; i++)
            {
                int minDistance = int.MaxValue;
                int minIndex = -1;

                for (int j = 0; j < size; j++)
                {
                    if (!visited[j] && distance[j] < minDistance)
                    {
                        minDistance = distance[j];
                        minIndex = j;
                    }
                }

                if (minIndex == -1) break;

                visited[minIndex] = true;

               
                for (int j = 0; j < size; j++)
                {
                    if (graph[minIndex, j] > 0 && !visited[j])
                    {
                        int newDist = distance[minIndex] + graph[minIndex, j];
                        if (newDist < distance[j])
                        {
                            distance[j] = newDist;
                            previous[j] = minIndex;
                        }
                    }
                }
            }

         
            List<int> path = new List<int>();
            int current = target;
            while (current != -1)
            {
                path.Add(current);
                current = previous[current];
            }

            path.Reverse();
            return (path, distance[target]);
        }
        private void ExtinguishFireAtCurrentLocation()
        {
            int regionIndex = Array.FindIndex(regionCoordinates, p => p == currentLocation);

            if (regionIndex != -1 && fireIntensity[regionIndex] > 0)
            {
                int currentFireLevel = fireIntensity[regionIndex];

                while (currentFireLevel > 0)
                {

                    int waterNeeded = 1000; 
                    int timeNeeded = fireTime[currentFireLevel - 1]; 
                    int fuelNeeded = timeNeeded * 10; 
                   
                    if (water < waterNeeded || fuel < fuelNeeded)
                    {
                       
                        fireIntensity[regionIndex] = currentFireLevel;
                        var pathToBase = Dijkstra(graphMatrix, regionIndex, 0).path;
                        StartAnimation(pathToBase);
                        return;
                    }

                  
                    water -= waterNeeded;
                    fuel -= fuelNeeded;
                    totalWaterUsed += waterNeeded;
                    totalFuelUsed += fuelNeeded;

                    
                    currentFireLevel--;
                    fireIntensity[regionIndex] = currentFireLevel;

                    
                    pnlStatus.Invalidate();
                    pnlMap.Invalidate();

                    string message = currentFireLevel > 0
                        ? $"Bölge B{regionIndex} yangın şiddeti {currentFireLevel}'e düştü.\nKullanılan su: {waterNeeded} lt\nKullanılan yakıt: {fuelNeeded} lt"
                        : $"Bölge B{regionIndex} tamamen söndürüldü!\nKullanılan su: {waterNeeded} lt\nKullanılan yakıt: {fuelNeeded} lt";

                    MessageBox.Show(message, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                if (currentFireLevel == 0)
                {
                    extinguishedFires.Add(regionIndex);
                    FindNextTarget();
                }
            }
        }


        private void FindNextTarget()
        {
            List<int> remainingFires = new List<int>();
            for (int i = 1; i < fireIntensity.Length; i++)
            {
                if (fireIntensity[i] > 0)
                {
                    remainingFires.Add(i);
                }
            }

            if (remainingFires.Count > 0)
            {
                int currentRegionIndex = Array.FindIndex(regionCoordinates, p => p == currentLocation);
                int nextTarget = GetBestNextTarget(remainingFires, currentRegionIndex);

                if (nextTarget != -1)
                {
                    var path = Dijkstra(graphMatrix, currentRegionIndex, nextTarget).path;
                    StartAnimation(path);
                }
                else
                {
                    
                    var pathToBase = Dijkstra(graphMatrix, currentRegionIndex, 0).path;
                    StartAnimation(pathToBase);
                }
            }
            else
            {
                MessageBox.Show("Tüm yangınlar söndürüldü!", "Tebrikler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowAndSaveFinalReport();

            }
        }
        private int GetBestNextTarget(List<int> remainingFires, int currentPosition)
        {
            int bestTarget = -1;
            int shortestDistance = int.MaxValue;

            foreach (int fireRegion in remainingFires)
            {
                var pathInfo = Dijkstra(graphMatrix, currentPosition, fireRegion);
                int fireLevel = fireIntensity[fireRegion];

                int fuelForTravel = pathInfo.distance;
               
                int fuelForExtinguish = fireTime[fireLevel - 1] * 10;
                
                int totalFuelNeeded = fuelForTravel + fuelForExtinguish;

                
                int waterNeeded = 1000 * fireLevel;

                
                if (fuel >= totalFuelNeeded &&
                    water >= waterNeeded &&
                    pathInfo.distance < shortestDistance)
                {
                    shortestDistance = pathInfo.distance;
                    bestTarget = fireRegion;
                }
            }

            return bestTarget;
        }
        private void ShowAndSaveFinalReport()
        {
            
            string report = $"Yangın Söndürme Raporu:\n\n" +
                            $"- Toplam Mesafe: {totalDistanceTravelled} km\n" +
                            $"- Toplam Su Kullanımı: {totalWaterUsed} litre\n" +
                            $"- Toplam Yakıt Kullanımı: {totalFuelUsed} litre\n" +
                            $"- Yakıt Dolumu Sayısı: {refuelCount}\n" +
                            $"- Su Dolumu Sayısı: {waterRefillCount}\n" +
                            $"- Söndürülen Yangın Bölgeleri Sırası: {string.Join(", ", extinguishedFires)}";

           
            MessageBox.Show(report, "Rapor", MessageBoxButtons.OK, MessageBoxIcon.Information);

           
            string filePath = @"C:\Users\hamza\source\repos\YepyeniOrman\YepyeniOrman\bin\Debug\sonuc.txt";

            try
            {
             
                File.WriteAllText(filePath, report);

               
                MessageBox.Show($"Rapor başarıyla {filePath} dosyasına kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
               
                MessageBox.Show($"Dosya kaydedilirken bir hata oluştu: {ex.ToString()}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ReturnToBase()
        {
            int currentRegionIndex = Array.FindIndex(regionCoordinates, p => p == currentLocation);
            var pathToBase = Dijkstra(graphMatrix, currentRegionIndex, 0).path;

            if (currentRegionIndex != 0) 
            {
                StartAnimation(pathToBase);
            }

            water = waterCapacity;
            fuel = fuelCapacity;
            waterRefillCount++;
            refuelCount++;

            MessageBox.Show($"Yakıt ve su dolduruldu!\nYakıt: {fuel} lt\nSu: {water} lt",
                          "Dolum Tamamlandı",
                           MessageBoxButtons.OK,
                         MessageBoxIcon.Information);

            FindNextTarget();
        }

        private int GetNearestFireRegion(List<int> fireRegions)
            {
                int nearestRegion = fireRegions[0];
                int shortestDistance = int.MaxValue;

                foreach (int region in fireRegions)
                {
                    var result = Dijkstra(graphMatrix, 0, region);
                    if (result.distance < shortestDistance)
                    {
                        shortestDistance = result.distance;
                        nearestRegion = region;
                    }
                }
                return nearestRegion;
            }

            private void RefuelAndRefill()
            {
                water = waterCapacity;
                fuel = fuelCapacity;
                waterRefillCount++;
                refuelCount++;
                 MessageBox.Show("Yakıt ve su dolduruldu!", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        private void UpdateRouteAndHandleRemainingFires()
        {
            List<int> remainingFires = new List<int>();
            for (int i = 1; i < fireIntensity.Length; i++)
            {
                if (fireIntensity[i] > 0)
                {
                    remainingFires.Add(i);
                }
            }

            if (remainingFires.Count > 0)
            {
                int nextFireRegion = GetOptimalNextFireRegion(remainingFires);
                if (nextFireRegion != -1)
                {
                    var shortestPath = Dijkstra(graphMatrix, Array.FindIndex(regionCoordinates, p => p == currentLocation), nextFireRegion);
                    StartAnimation(shortestPath.path);
                }
                else
                {
                    ReturnToBase();
                }
            }
            else
            {
                MessageBox.Show("Tüm yangınlar başarıyla söndürüldü!", "Tebrikler", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowAndSaveFinalReport();
            }
        }
        private int GetOptimalNextFireRegion(List<int> fireRegions)
        {
            int currentRegionIndex = Array.FindIndex(regionCoordinates, p => p == currentLocation);
            int optimalRegion = -1;
            int shortestDistance = int.MaxValue;

            foreach (int region in fireRegions)
            {
                if (fireIntensity[region] == 0) continue; 

                var pathInfo = Dijkstra(graphMatrix, currentRegionIndex, region);
                int requiredWater = waterRequired[fireIntensity[region] - 1];
                int requiredFuel = (fireTime[fireIntensity[region] - 1] * 10) + pathInfo.distance;

                
                if (water >= requiredWater && fuel >= requiredFuel && pathInfo.distance < shortestDistance)
                {
                    shortestDistance = pathInfo.distance;
                    optimalRegion = region;
                }
            }

            return optimalRegion;
        }
        private void btnStart_Click_1(object sender, EventArgs e)
        {
            
            currentLocation = regionCoordinates[0];

            
            List<int> activeFireRegions = new List<int>();
            for (int i = 1; i < fireIntensity.Length; i++)
            {
                if (fireIntensity[i] > 0)
                {
                    activeFireRegions.Add(i);
                }
            }

            if (activeFireRegions.Count > 0)
            {
                int firstTarget = GetBestNextTarget(activeFireRegions, 0);
                if (firstTarget != -1)
                {
                    var path = Dijkstra(graphMatrix, 0, firstTarget).path;
                    StartAnimation(path);
                }
                else
                {
                    MessageBox.Show("Başlangıç için yeterli kaynak yok!",
                                  "Hata",
                                  MessageBoxButtons.OK,
                                  MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Söndürülecek yangın bulunmuyor.",
                              "Bilgi",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Information);
            }
        }

        private void pnlStatus_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            
            float waterPercentage = (float)water / waterCapacity;
            g.FillRectangle(Brushes.Blue, 10, 10, waterPercentage * (pnlStatus.Width - 20), 20);
            g.DrawRectangle(Pens.Black, 10, 10, pnlStatus.Width - 20, 20);
            g.DrawString($"Su: {water}/{waterCapacity} ({(int)(waterPercentage * 100)}%)",
                         this.Font, Brushes.Black, 10, 35);

        
            float fuelPercentage = (float)fuel / fuelCapacity;
            g.FillRectangle(Brushes.Orange, 10, 60, fuelPercentage * (pnlStatus.Width - 20), 20);
            g.DrawRectangle(Pens.Black, 10, 60, pnlStatus.Width - 20, 20);
            g.DrawString($"Yakıt: {fuel}/{fuelCapacity} ({(int)(fuelPercentage * 100)}%)",
                         this.Font, Brushes.Black, 10, 85);
        }

    }
}
