using UnityEngine; // 仅用于 Debug.LogError，可以移除或替换

public class ThermodynamicsCalculator
{
    // 摩尔气体常数 R, 单位 J/(mol·K)
    private readonly float molarGasConstantR = 8.314f;

    // --- 单位换算常量 ---
    // 1 立方厘米 (cm³) = 10⁻⁶ 立方米 (m³)
    private const float CM3_TO_M3_CONVERSION = 1.0e-6f;
    // 1 标准大气压 (atm) = 101325 帕斯卡 (Pa)
    private const float ATM_TO_PA_CONVERSION = 101325f;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="gasConstantR_JoulesPerMolKelvin">摩尔气体常数 R，标准值为 8.314 J/(mol·K)</param>
    public ThermodynamicsCalculator()
    {
        // this.molarGasConstantR = gasConstantR_JoulesPerMolKelvin;
        //
        // if (this.molarGasConstantR <= float.Epsilon)
        // {
        //     Debug.LogError("ThermodynamicsCalculator: Molar gas constant (R) must be a positive value.");
        // }
    }

    /// <summary>
    /// 1. 根据温度和体积，计算压强 P
    /// 公式: P = nRT / V
    /// </summary>
    /// <param name="moleAmount">物质的量 n (单位: mol)</param>
    /// <param name="volume_cm3">体积 V (单位: cm³)</param>
    /// <param name="temperature_K">温度 T (单位: K)</param>
    /// <returns>计算得到的压强 (单位: atm)</returns>
    public float CalculatePressure(float moleAmount, float volume_cm3, float temperature_K)
    {
        // 安全性检查
        if (volume_cm3 <= float.Epsilon) return 0f;
        if (moleAmount <= 0f || temperature_K <= 0f) return 0f;
        if (molarGasConstantR <= float.Epsilon) return 0f;

        // 将体积从 cm³ 转换为 m³
        float volume_m3 = volume_cm3 * CM3_TO_M3_CONVERSION;
        
        // 使用SI单位计算压强 (结果是 Pa)
        float pressure_Pa = (moleAmount * molarGasConstantR * temperature_K) / volume_m3;
        
        // 将计算结果从 Pa 转换为 atm 返回
        return pressure_Pa / ATM_TO_PA_CONVERSION;
    }

    /// <summary>
    /// 2. 根据温度和压强，计算体积 V
    /// 公式: V = nRT / P
    /// </summary>
    /// <param name="moleAmount">物质的量 n (单位: mol)</param>
    /// <param name="temperature_K">温度 T (单位: K)</param>
    /// <param name="pressure_atm">压强 P (单位: atm)</param>
    /// <returns>计算得到的体积 (单位: cm³)</returns>
    public float CalculateVolume(float moleAmount, float temperature_K, float pressure_atm)
    {
        // 安全性检查
        if (pressure_atm <= float.Epsilon) return float.PositiveInfinity;
        if (moleAmount <= 0f || temperature_K <= 0f) return 0f;
        if (molarGasConstantR <= float.Epsilon) return 0f;

        // 将输入的压强从 atm 转换为 Pa，用于内部计算
        float pressure_Pa = pressure_atm * ATM_TO_PA_CONVERSION;

        // 使用SI单位计算体积 (结果是 m³)
        float volume_m3 = (moleAmount * molarGasConstantR * temperature_K) / pressure_Pa;
        
        // 将结果从 m³ 转换回 cm³ 返回
        return volume_m3 / CM3_TO_M3_CONVERSION;
    }

    /// <summary>
    /// 3. 根据体积和压强，计算温度 T
    /// 公式: T = PV / (nR)
    /// </summary>
    /// <param name="moleAmount">物质的量 n (单位: mol)</param>
    /// <param name="volume_cm3">体积 V (单位: cm³)</param>
    /// <param name="pressure_atm">压强 P (单位: atm)</param>
    /// <returns>计算得到的温度 (单位: K)</returns>
    public float CalculateTemperature(float moleAmount, float volume_cm3, float pressure_atm)
    {
        // 安全性检查
        if (moleAmount <= float.Epsilon) return 0f;
        if (volume_cm3 < 0f || pressure_atm < 0f) return 0f;
        if (molarGasConstantR <= float.Epsilon) return 0f;

        // 将输入的压强从 atm 转换为 Pa
        float pressure_Pa = pressure_atm * ATM_TO_PA_CONVERSION;
        // 将体积从 cm³ 转换为 m³
        float volume_m3 = volume_cm3 * CM3_TO_M3_CONVERSION;

        // 使用SI单位计算温度
        return (pressure_Pa * volume_m3) / (moleAmount * molarGasConstantR);
    }
}