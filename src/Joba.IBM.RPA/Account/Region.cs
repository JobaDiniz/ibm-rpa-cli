﻿namespace Joba.IBM.RPA
{
    public record class Region(string Name, Uri ApiUrl, string? Description = null)
    {
        public override string ToString() => ApiUrl.ToString();
    }
}
