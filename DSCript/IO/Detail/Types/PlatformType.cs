namespace DSCript
{
    public enum PlatformType : int
    {
        PC      = 0,
        
        Console = 1,
            PS2     = (Console + 1),
            Xbox    = (Console + 2),
        
        Mobile  = 4,
            PSP     = (Mobile + 1),
        
        NextGen = 8,
            PS3     = (NextGen + 1),
            Xbox360 = (NextGen + 2),
            Wii     = (NextGen + 3),
        
        Any     = -1,
    }
}
