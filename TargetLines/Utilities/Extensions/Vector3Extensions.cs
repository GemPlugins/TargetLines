namespace TargetLines.Utilities.Extensions;

using DrahsidLib;
using System.Numerics;

public static class Vector3Extensions
{
    public static unsafe CSVector3 CSVector3(this SharpDX.Vector3 thisx)
    {
        return *(CSVector3*)(&thisx.X);
    }

    public static unsafe SharpDX.Vector3 DXVector3(this CSVector3 thisx)
    {
        return *(SharpDX.Vector3*)(&thisx.X);
    }

    public static unsafe Vector3 Vector3(this SharpDX.Vector3 thisx)
    {
        return *(Vector3*)(&thisx.X);
    }

    public static unsafe Vector3 Vector3(this CSVector3 thisx)
    {
        return *(Vector3*)(&thisx.X);
    }

    public static unsafe CSVector3 CSVector3(this Vector3 thisx)
    {
        return *(CSVector3*)(&thisx.X);
    }

    public static unsafe SharpDX.Vector3 DXVector3(this Vector3 thisx)
    {
        return *(SharpDX.Vector3*)(&thisx.X);
    }

    public static void Tests()
    {
        float[] Numerics_XYZ = { 1, 2, 3 };
        float[] Structs_XYZ = { 4, 5, 6 };
        float[] SDX_XYZ = { 7, 8, 9 };

        Vector3 Numerics = new Vector3(Numerics_XYZ[0], Numerics_XYZ[1], Numerics_XYZ[2]);
        CSVector3 Structs = new CSVector3(Structs_XYZ[0], Structs_XYZ[1], Structs_XYZ[2]);
        SharpDX.Vector3 SDX = new SharpDX.Vector3(SDX_XYZ[0], SDX_XYZ[1], SDX_XYZ[2]);

        CSVector3 NumericsToStructs = Numerics.CSVector3();
        SharpDX.Vector3 NumericsToSDX = Numerics.DXVector3();
        Vector3 StructsToNumerics = Structs.Vector3();
        SharpDX.Vector3 StructsToSDX = Structs.DXVector3();
        Vector3 SDXToNumerics = SDX.Vector3();
        CSVector3 SDXToStructs = SDX.CSVector3();

        if (NumericsToStructs != new CSVector3(Numerics_XYZ[0], Numerics_XYZ[1], Numerics_XYZ[2]))
        {
            throw new System.Exception("NumericsToStructs bogus!!!");
        }

        if (NumericsToSDX != new SharpDX.Vector3(Numerics_XYZ[0], Numerics_XYZ[1], Numerics_XYZ[2]))
        {
            throw new System.Exception("NumericsToSDX bogus!!!");
        }

        if (StructsToNumerics != new Vector3(Structs_XYZ[0], Structs_XYZ[1], Structs_XYZ[2]))
        {
            throw new System.Exception("StructsToNumerics bogus!!!");
        }

        if (StructsToSDX != new SharpDX.Vector3(Structs_XYZ[0], Structs_XYZ[1], Structs_XYZ[2]))
        {
            throw new System.Exception("StructsToSDX bogus!!!");
        }

        if (SDXToNumerics != new Vector3(SDX_XYZ[0], SDX_XYZ[1], SDX_XYZ[2]))
        {
            throw new System.Exception("SDXToNumerics bogus!!!");
        }

        if (SDXToStructs != new CSVector3(SDX_XYZ[0], SDX_XYZ[1], SDX_XYZ[2]))
        {
            throw new System.Exception("SDXToStructs bogus!!!");
        }
    }
}
