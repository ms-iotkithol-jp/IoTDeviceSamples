using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class Thing {
    public double CurrentTemperature { get; set; }
    public double TargetTemperature { get; set; }
    public double CurrentHumidity { get; set; }
    public double TargetHumidity {
        get { return targetHumidity; }
        set {
            targetHumidity = value;
            humDelta = System.Math.Abs ((targetHumidity - CurrentHumidity) / HumDeltaRound);
        }
    }
    public double CurrentAccelX { get; set; }
    public double CurrentAccelY { get; set; }
    public double CurrentAccelZ { get; set; }
    public double AccelWhiteNoiseRate { get; set; }
    public double MaxAccelValue { get; set; }
    public double TempWhiteNoiseRate { get; set; }
    public double TempDeltaCoef { get; set; }
    public double HumDeltaRound { get; set; }
    public DateTime dataFixedTime { get; set; }
    public ThingStatus Status { get; set; }
    public ThingFace FaceTop { get; set; }

    private double targetHumidity;
    Random tRand;
    Random aRand;

    bool toContinue;
    public Thing () {
        tRand = new Random (DateTime.Now.Millisecond);
        aRand = new Random (DateTime.Now.Millisecond);
        faceSideVectors = new Dictionary<ThingFace, double[]> ();
        faceSideVectors.Add (ThingFace.F1, new double[] { 0, 0, -1 });
        faceSideVectors.Add (ThingFace.F2, new double[] {-1, 0, 0 });
        faceSideVectors.Add (ThingFace.F3, new double[] { 0, -1, 0 });
        faceSideVectors.Add (ThingFace.F4, new double[] { 0, 1, 0 });
        faceSideVectors.Add (ThingFace.F5, new double[] { 1, 0, 0 });
        faceSideVectors.Add (ThingFace.F6, new double[] { 0, 0, 1 });
    }

    public void Initialize (int msecForLoop) {
        FaceTop = ThingFace.F1;
        CurrentAccelX = 0;
        CurrentAccelY = 0;
        CurrentAccelZ = -1;
        CurrentTemperature = TargetTemperature;
        CurrentHumidity = TargetHumidity;
        toContinue = true;
        dataFixedTime = DateTime.Now;
        Task.Factory.StartNew (() => {
            UpdateValue (msecForLoop).Wait ();
        });
    }

    public void Terminate () {
        lock (this) {
            toContinue = false;
        }
    }

    public SensorReading Read () {
        SensorReading sr = null;

        lock (this) {
            sr = new SensorReading () {
                accelerometerx = CurrentAccelX,
                accelerometery = CurrentAccelY,
                accelerometerz = CurrentAccelZ,
                temperature = CurrentTemperature,
                humidity = CurrentHumidity,
                time = dataFixedTime
            };
        }
        return sr;
    }

    public void Shake (double targetTemp) {
        lock (this) {
            Status = ThingStatus.SHAKE;
            TargetTemperature = targetTemp;
        }
    }

    public void Put (double targetTemp, ThingFace face = ThingFace.F1) {
        lock (this) {
            Status = ThingStatus.STABLE;
            FaceTop = face;
            TargetTemperature = targetTemp;
        }
    }

    Dictionary<ThingFace, double[]> faceSideVectors;
    double humDelta = 0.0;

    async Task UpdateValue (int msecForLoop) {
        while (true) {
            lock (this) {
                bool isEnding = !toContinue;
                if (isEnding) break;

                if (Status == ThingStatus.SHAKE) {
                    CurrentAccelX = 2 * MaxAccelValue * (aRand.NextDouble () - 0.5);
                    CurrentAccelY = 2 * MaxAccelValue * (aRand.NextDouble () - 0.5);
                    CurrentAccelZ = 2 * MaxAccelValue * (aRand.NextDouble () - 0.5);
                } else {
                    CurrentAccelX = faceSideVectors[FaceTop][0] + 2 * AccelWhiteNoiseRate * (aRand.NextDouble () - 0.5);
                    CurrentAccelY = faceSideVectors[FaceTop][1] + 2 * AccelWhiteNoiseRate * (aRand.NextDouble () - 0.5);
                    CurrentAccelZ = faceSideVectors[FaceTop][2] + 2 * AccelWhiteNoiseRate * (aRand.NextDouble () - 0.5);
                }
                double dT = TempDeltaCoef * (TargetTemperature - CurrentTemperature);
                CurrentTemperature += dT + 2 * TempWhiteNoiseRate * (tRand.NextDouble () - 0.5) * ((double) msecForLoop) / 1000.0;
                if (System.Math.Abs (TargetHumidity - CurrentHumidity) != 0) {
                    if (TargetHumidity > CurrentHumidity) {
                        CurrentHumidity += humDelta;
                        if (CurrentHumidity > TargetHumidity) {
                            CurrentHumidity = TargetHumidity;
                        }
                    } else {
                        CurrentHumidity -= humDelta;
                        if (CurrentHumidity < TargetHumidity) {
                            CurrentHumidity = TargetHumidity;
                        }
                    }
                }
                dataFixedTime = DateTime.Now;
            }
            await Task.Delay (msecForLoop);
        }
    }
}

public enum ThingStatus {
    STABLE = 0,
    SHAKE = 1
}

public enum ThingFace {
    F1,
    F2,
    F3,
    F4,
    F5,
    F6
}

public class SensorReading {
    public double accelerometerx { get; set; }
    public double accelerometery { get; set; }
    public double accelerometerz { get; set; }
    public double temperature { get; set; }
    public double humidity { get; set; }
    public DateTime time { get; set; }
}