using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantMicroService
{
    using CommonMessages;
    using EasyNetQ.Events;
    using QLNet;

    /// <summary>   A common variables. </summary>
    class CommonVars
    {
        /// <summary>   common data. </summary>
        public Calendar calendar;
        /// <summary>   The today. </summary>
        public Date today;
        /// <summary>   The face amount. </summary>
        public double faceAmount;

        /// <summary>   setup. </summary>
        public CommonVars()
        {
            calendar = new TARGET();
            today = calendar.adjust(Date.Today);
            Settings.setEvaluationDate(today);
            faceAmount = 1000000.0;
        }
    }

    /// <summary>   A bonds. </summary>
    public class Bonds
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Tests yield. </summary>
        ///
        /// <param name="ms">   The milliseconds. </param>
        ///
        /// <returns>   True if the test passes, false if the test fails. </returns>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool testYield(QuantMicroService ms)
        {


            //"Testing consistency of bond price/yield calculation...");

            CommonVars vars = new CommonVars();

            double tolerance = 1.0e-7;
            int maxEvaluations = 100;

            int[] issueMonths = new int[] { -24, -18, -12, -6, 0, 6, 12, 18, 24 };
            int[] lengths = new int[] { 3, 5, 10, 15, 20 };
            int settlementDays = 3;
            double[] coupons = new double[] { 0.02, 0.05, 0.08 };
            Frequency[] frequencies = new Frequency[] { Frequency.Semiannual, Frequency.Annual };
            DayCounter bondDayCount = new Thirty360();
            BusinessDayConvention accrualConvention = BusinessDayConvention.Unadjusted;
            BusinessDayConvention paymentConvention = BusinessDayConvention.ModifiedFollowing;
            double redemption = 100.0;

            double[] yields = new double[] { 0.03, 0.04, 0.05, 0.06, 0.07 };
            Compounding[] compounding = new Compounding[] { Compounding.Compounded, Compounding.Continuous };

            foreach (var t in issueMonths)
            {
                foreach (var t1 in lengths)
                {
                    foreach (var t2 in coupons)
                    {
                        foreach (var t3 in frequencies)
                        {
                            foreach (var t4 in compounding)
                            {
                                Date dated = vars.calendar.advance(vars.today, t, TimeUnit.Months);
                                Date issue = dated;
                                Date maturity = vars.calendar.advance(issue, t1, TimeUnit.Years);

                                Schedule sch = new Schedule(dated, maturity, new Period(t3), vars.calendar,
                                   accrualConvention, accrualConvention, DateGeneration.Rule.Backward, false);

                                FixedRateBond bond = new FixedRateBond(settlementDays, vars.faceAmount, sch,
                                   new List<double>() { t2 }, bondDayCount, paymentConvention, redemption, issue);

                                foreach (var t5 in yields)
                                {
                                    double price = bond.cleanPrice(t5, bondDayCount, t4, t3);
                                    double calculated = bond.yield(price, bondDayCount, t4, t3, null,
                                       tolerance, maxEvaluations);

                                    double price2 = bond.cleanPrice(calculated, bondDayCount, t4, t3);
                                    BondsResponseMessage r = new BondsResponseMessage();
                                    r.message = (Math.Abs(price - price2) / price > tolerance) ?
                                        "yield recalculation failed:" : "";
                                    r.issue = issue;
                                    r.maturity = maturity;
                                    r.coupon = t2;
                                    r.frequency = (int)t3;
                                    r.yield = t5;
                                    r.compounding = (t4 == Compounding.Compounded
                                        ? "compounded" : "continuous");
                                    r.price = price;
                                    r.price2 = price2;
                                    r.calcYield = calculated;

                                    ms.PublishBondResponseMessage(r, "BondResponse");

                                    if (Math.Abs(t5 - calculated) > tolerance)
                                    {
                                        return (Math.Abs(price - price2) / price > tolerance);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (true);
        }
    }
}
