using QuantConnect;
using QuantConnect.Data;
using QuantConnect.Algorithm;

namespace QuantConnect.Algorithm.CSharp.MyAlgorithms
{
    public class MyFirstAlgorithm : QCAlgorithm
    {
        public override void Initialize()
        {
            // 回测时间范围
            SetStartDate(2020, 1, 1);
            SetEndDate(2025, 10, 1);

            // 初始资金
            SetCash(10000);

            // 先用 JNJ，利用仓库自带/已有数据
            AddEquity("JNJ", Resolution.Daily);
        }

        public override void OnData(Slice data)
        {
            if (data.ContainsKey("JNJ"))
            {
                Debug($"[{Time}] JNJ Price: {Securities["JNJ"].Price}");
            }

            if (!Portfolio.Invested && data.ContainsKey("JNJ"))
            {
                Debug($"[{Time}] Attempting to buy JNJ at {Securities["JNJ"].Price}");
                SetHoldings("JNJ", 1);
            }
        }
    }
}
