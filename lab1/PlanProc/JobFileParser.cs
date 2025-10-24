namespace PlanProc
{
    public class ProcessInfo
    {
        public string Name { get; init; }
        public string MatrixFile { get; init; }
        public string VectorFile { get; init; }
        public int ArrivalTime { get; init; }
    }

    public static class JobFileParser
    {
        public static List<ProcessInfo> Parse(string filename)
        {
            if (!File.Exists(filename))
            {
                Console.WriteLine($"Ошибка: файл заданий '{filename}' не найден.");
                return null;
            }

            try
            {
                var lines = File.ReadAllLines(filename);
                int? expectedCount = null;
                var processesData = new List<ProcessInfo>();

                foreach (var line in lines)
                {
                    string trimmedLine = line.Trim();
                    if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#")) continue;

                    if (trimmedLine.StartsWith("q="))
                    {
                        var parts = trimmedLine.Split('=');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int q))
                        {
                            expectedCount = q;
                        }
                        else
                        {
                            Console.WriteLine($"Ошибка: некорректное значение количества процессов в строке '{trimmedLine}'.");
                            return null;
                        }
                        continue;
                    }

                    var lineParts = trimmedLine.Split();
                    if (lineParts.Length != 4)
                    {
                        Console.WriteLine($"Ошибка: некорректный формат строки в файле '{filename}': '{trimmedLine}'.");
                        return null;
                    }

                    string name = lineParts[0].TrimEnd(':');
                    string matrixName = lineParts[1];
                    string vectorName = lineParts[2];
                    var timePart = lineParts[3].Split('=');

                    if (timePart.Length != 2 || timePart[0] != "t" || !int.TryParse(timePart[1], out int arrivalTime))
                    {
                        Console.WriteLine($"Ошибка: некорректный формат времени в строке '{trimmedLine}'.");
                        return null;
                    }

                    processesData.Add(new ProcessInfo
                    {
                        Name = name,
                        MatrixFile = $"{matrixName}.txt",
                        VectorFile = $"{vectorName}.txt",
                        ArrivalTime = arrivalTime
                    });
                }

                if (expectedCount.HasValue && processesData.Count != expectedCount.Value)
                {
                    Console.WriteLine($"Ошибка: ожидаемое количество процессов {expectedCount.Value}, но найдено {processesData.Count}.");
                    return null;
                }

                return processesData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка чтения файла '{filename}': {ex.Message}");
                return null;
            }
        }
    }
}