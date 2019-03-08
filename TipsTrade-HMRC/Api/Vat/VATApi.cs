﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TipsTrade.HMRC.AntiFraud;
using TipsTrade.HMRC.Api.Vat.Model;

namespace TipsTrade.HMRC.Api.Vat {
  /// <summary>The API that exposes VAT functions.</summary>
  public class VatApi : IApi, IClient, IRequiresAntiFraud {
    private const string FuelScaleChargesResource = "Resources.FuelScaleCharges.json";

    #region Error constants
    ///<summary>
    ///The client and/or agent is not authorised.
    ///</summary>
    public const string ERROR_CLIENT_OR_AGENT_NOT_AUTHORISED = "CLIENT_OR_AGENT_NOT_AUTHORISED";

    ///<summary>
    ///Invalid date from
    ///</summary>
    public const string ERROR_DATE_FROM_INVALID = "DATE_FROM_INVALID";

    ///<summary>
    ///Invalid date range, must be 1 year or less
    ///</summary>
    public const string ERROR_DATE_RANGE_INVALID = "DATE_RANGE_INVALID";

    ///<summary>
    ///The date of the requested return cannot be more than four years from the current date
    ///</summary>
    public const string ERROR_DATE_RANGE_TOO_LARGE = "DATE_RANGE_TOO_LARGE";

    ///<summary>
    ///Invalid date to
    ///</summary>
    public const string ERROR_DATE_TO_INVALID = "DATE_TO_INVALID";

    ///<summary>
    ///User has already submitted a VAT return for the given period
    ///</summary>
    public const string ERROR_DUPLICATE_SUBMISSION = "DUPLICATE_SUBMISSION";

    ///<summary>
    ///Invalid date from
    ///</summary>
    public const string ERROR_INVALID_DATE_FROM = "INVALID_DATE_FROM";

    ///<summary>
    ///Invalid date range
    ///</summary>
    public const string ERROR_INVALID_DATE_RANGE = "INVALID_DATE_RANGE";

    ///<summary>
    ///Invalid date to
    ///</summary>
    public const string ERROR_INVALID_DATE_TO = "INVALID_DATE_TO";

    ///<summary>
    ///amounts should be a non-negative number less than 9999999999999.99 with up to 2 decimal places
    ///The value must be between -9999999999999 and 9999999999999
    ///amount should be a monetary value (to 2 decimal places), between 0 and 99999999999.99
    ///</summary>
    public const string ERROR_INVALID_MONETARY_AMOUNT = "INVALID_MONETARY_AMOUNT";

    ///<summary>
    ///please provide a numeric field
    ///</summary>
    public const string ERROR_INVALID_NUMERIC_VALUE = "INVALID_NUMERIC_VALUE";

    ///<summary>
    ///Invalid request
    ///</summary>
    public const string ERROR_INVALID_REQUEST = "INVALID_REQUEST";

    ///<summary>
    ///Invalid status
    ///</summary>
    public const string ERROR_INVALID_STATUS = "INVALID_STATUS";

    ///<summary>
    ///User has not declared VAT return as final
    ///</summary>
    public const string ERROR_NOT_FINALISED = "NOT_FINALISED";

    ///<summary>
    ///The remote endpoint has indicated that no associated data is found
    ///</summary>
    public const string ERROR_NOT_FOUND = "NOT_FOUND";

    ///<summary>
    ///Invalid period key
    ///</summary>
    public const string ERROR_PERIOD_KEY_INVALID = "PERIOD_KEY_INVALID";

    ///<summary>
    ///Return submitted too early
    ///</summary>
    public const string ERROR_TAX_PERIOD_NOT_ENDED = "TAX_PERIOD_NOT_ENDED";

    ///<summary>
    ///netVatDue should be the difference between the largest and the smallest values among totalVatDue and vatReclaimedCurrPeriod
    ///</summary>
    public const string ERROR_VAT_NET_VALUE = "VAT_NET_VALUE";

    ///<summary>
    ///totalVatDue should be equal to the sum of vatDueSales and vatDueAcquisitions
    ///</summary>
    public const string ERROR_VAT_TOTAL_VALUE = "VAT_TOTAL_VALUE";

    ///<summary>
    ///Invalid VRN
    ///The provided VRN is invalid
    ///</summary>
    public const string ERROR_VRN_INVALID = "VRN_INVALID";

    #endregion

    #region Properties
    /// <summary>The client used to make requests.</summary>
    Client IClient.Client { get; set; }

    /// <summary>The description of the API.</summary>
    public string Description => "An API for providing VAT data.";

    /// <summary>A flag indicating whether this version of the API is stable.</summary>
    public bool IsStable => false;

    /// <summary>The relative location of the API.</summary>
    public string Location => "organisations/vat";

    /// <summary>The name of the API.</summary>
    public string Name => "VAT (MTD) API";

    /// <summary>The version of the API that the client should target.</summary>
    public string Version => "1.0";
    #endregion

    #region Methods
    /// <summary>
    /// Gets the fuel scale charge from the live HMRC website.
    /// Deprecated, used the <see cref="GetFuelScaleChargeFromCO2Live(DateTime, VatPeriod, int)"/> method instead.
    /// </summary>
    /// <param name="date">The accounting period for which the scale charge should be retrieved.</param>
    /// <param name="periodLength">The length of the VAT period in months (1, 3, 12).</param>
    /// <param name="co2">The CO2 emmissions (g/km) of the vehicle.</param>
    [Obsolete]
    public static FuelScaleChargeResult GetFuelScaleChargeFromCO2Live(DateTime date, byte periodLength, int co2) {
      var client = new FuelScaleChargeClient();
      return client.GetFuelScaleChargeFromCO2(date, periodLength, co2);
    }

    /// <summary>Gets the fuel scale charge from the live HMRC website.</summary>
    /// <param name="date">The accounting period for which the scale charge should be retrieved.</param>
    /// <param name="period">The length of the VAT period.</param>
    /// <param name="co2">The CO2 emmissions (g/km) of the vehicle.</param>
    public static FuelScaleChargeResult GetFuelScaleChargeFromCO2Live(DateTime date, VatPeriod period, int co2) {
      var client = new FuelScaleChargeClient();
      return client.GetFuelScaleChargeFromCO2(date, period, co2);
    }

    /// <summary>
    /// Gets the fuel scale charge.
    /// Deprecated, used the <see cref="GetFuelScaleChargeFromCO2(DateTime, VatPeriod, int)"/> method instead.
    /// </summary>
    /// <param name="date">The accounting period for which the scale charge should be retrieved.</param>
    /// <param name="periodLength">The length of the VAT period in months (1, 3, 12).</param>
    /// <param name="co2">The CO2 emmissions (g/km) of the vehicle.</param>
    [Obsolete]
    public static FuelScaleChargeResult GetFuelScaleChargeFromCO2(DateTime date, byte periodLength, int co2) {
      if (!Enum.IsDefined(typeof(VatPeriod), periodLength))
        throw new ArgumentException($"{periodLength} is not valid.", nameof(periodLength));

      return GetFuelScaleChargeFromCO2(date, (VatPeriod)periodLength, co2);
    }

    /// <summary>Gets the fuel scale charge.</summary>
    /// <param name="date">The accounting period for which the scale charge should be retrieved.</param>
    /// <param name="period">The length of the VAT period.</param>
    /// <param name="co2">The CO2 emmissions (g/km) of the vehicle.</param>
    public static FuelScaleChargeResult GetFuelScaleChargeFromCO2(DateTime date, VatPeriod period, int co2) {
      var assembly = typeof(FuelScaleChargeResult).Assembly;
      var name = assembly.GetManifestResourceNames().Where(n => n.Contains(FuelScaleChargesResource)).First();

      FuelScaleChargeGroup[] values;
      using (var stream = assembly.GetManifestResourceStream(name)) {
        using (var reader = new StreamReader(stream)) {
          values = JsonConvert.DeserializeObject<FuelScaleChargeGroup[]>(reader.ReadToEnd());
        }
      }

      var group = values.Where(g => (g.From <= date) && (g.To >= date)).FirstOrDefault();
      if (group == null) {
        throw new InvalidOperationException($"No {nameof(FuelScaleChargeResult)} data could be found for {date}.");
      }

      IEnumerable<FuelScaleChargeResult> list;
      switch (period) {
        case VatPeriod.Month:
          list = group.Monthly;
          break;

        case VatPeriod.Quarter:
          list = group.Quarterly;
          break;

        case VatPeriod.Annual:
          list = group.Annually;
          break;

        default:
          throw new Exception("Already checked for.");

      }

      var result = list.Where(x => co2 <= x.CO2Band).First();

      result.From = group.From;
      result.To = group.To;

      return result;
    }

    /// <summary>Retrieve VAT liabilities.</summary>
    /// <param name="request">The date range request.</param>
    public LiabilitiesResponse GetLiabilities(LiabilitiesRequest request) {
      var restRequest = this.CreateRequest(request);

      return this.ExecuteRequest<LiabilitiesResponse>(restRequest);
    }

    /// <summary>Retrieve VAT obligations.</summary>
    /// <param name="request">The obligations request.</param>
    public ObligationResponse GetObligations(ObligationsRequest request) {
      var restRequest = this.CreateRequest(request);

      var resp = this.ExecuteRequest<ObligationResponse>(restRequest);

      // HACK: The Api appears to return all obligations, regardless of status, filter them here
      if (request.Status != null) {
        resp.Value = resp.Value.Except(resp.Value.Where(x => x.Status != request.Status));
      }

      return resp;
    }

    /// <summary>Retrieve VAT payments.</summary>
    /// <param name="request">The date range request.</param>
    public PaymentsResponse GetPayments(PaymentsRequest request) {
      var restRequest = this.CreateRequest(request);

      return this.ExecuteRequest<PaymentsResponse>(restRequest);
    }

    /// <summary>Retrieve a submitted VAT return.</summary>
    /// <param name="request">The retrieval request.</param>
    public ReturnResponse GetReturn(ReturnRequest request) {
      var restRequest = this.CreateRequest(request);

      return this.ExecuteRequest<ReturnResponse>(restRequest);
    }

    /// <summary>Submit VAT return for period.</summary>
    /// <param name="request">The submission request.</param>
    public SubmitResponse SubmitReturn(SubmitRequest request) {
      var restRequest = this.CreateRequest(request);

      return this.ExecuteRequest<SubmitResponse>(restRequest);
    }
    #endregion
  }
}
