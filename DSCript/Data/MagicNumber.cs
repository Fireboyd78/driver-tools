using System;

public struct MagicNumber
{
    public static readonly MagicNumber FIREBIRD = 0xF12EB12D;
    public static readonly MagicNumber FB       = 0xDF83;

    long m_value;

    public static implicit operator long(MagicNumber magic)
    {
        return magic.m_value;
    }

    public static implicit operator int(MagicNumber magic)
    {
        return (int)magic.m_value;
    }
    
    public static implicit operator MagicNumber(int value)
    {
        return new MagicNumber(value);
    }

    public static implicit operator MagicNumber(long value)
    {
        return new MagicNumber(value);
    }

    public static implicit operator MagicNumber(string value)
    {
        return new MagicNumber(value);
    }

    public MagicNumber(int value)
    {
        m_value = value;
    }

    public MagicNumber(long value)
    {
        m_value = (int)value;
    }

    public MagicNumber(string value)
    {
        if (value == null || value.Length > 8)
            throw new ArgumentException("Magic number strings cannot be null or greater than 8 characters long.", nameof(value));

        m_value = 0;

        for (int i = 0, shift = 0; i < value.Length; i++, shift += 8)
            m_value |= ((long)value[i] << shift);
    }
}