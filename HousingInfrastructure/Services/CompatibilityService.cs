using HousingDomain.Models;

namespace HousingInfrastructure.Services;

public static class CompatibilityService
{
    public static int Calculate(Profile a, Profile b)
    {
        int score = 0;
        int max = 7;

        if (a.NoiseLevel == b.NoiseLevel)
            score++;

        if (a.SleepMode == b.SleepMode)
            score++;

        if (a.Pets == b.Pets)
            score++;

        if (a.Guests == b.Guests)
            score++;

        if (a.CleanLevel == b.CleanLevel)
            score++;

        if (a.Smoking == b.Smoking)
            score++;

        if (a.PreferredGender == "Any" || a.PreferredGender == b.User.Gender)
            score++;

        return (score * 100) / max;
    }
}