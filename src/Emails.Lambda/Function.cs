using System.Globalization;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Shared.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Emails.Lambda;

public class Functions : IDisposable
{
    private readonly AmazonSimpleEmailServiceClient _sesClient;
    private readonly string _senderEmail;
    private bool _disposed;
    public Functions()
    {
        _sesClient = new AmazonSimpleEmailServiceClient();
        _senderEmail = Environment.GetEnvironmentVariable("SENDER_EMAIL") ?? throw new InvalidOperationException("SENDER_EMAIL environment variable not set.");
    }

    public async Task FunctionHandler(SQSEvent sqsEvent, ILambdaContext context, CancellationToken ct = default)
    {
        foreach (var message in sqsEvent.Records)
        {
            context.Logger.LogInformation($"Processing message ID: {message.MessageId}");
            var body = message.Body;
            if (string.IsNullOrWhiteSpace(body))
            {
                context.Logger.LogWarning("SQS message body was empty.");
                continue;
            }
            try
            {
                var itineraryEvent = JsonSerializer.Deserialize<ItineraryCreatedEvent>(body);
                if (itineraryEvent == null)
                {
                    context.Logger.LogWarning("Could not deserialize ItineraryCreatedEvent from message body.");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(itineraryEvent.LeadPassengerEmail))
                {
                    context.Logger.LogInformation(
                        $"No lead passenger email present for itinerary {itineraryEvent.Reference}.");
                    continue;
                }
                var sendRequest = new SendEmailRequest
                {
                    Source = _senderEmail,
                    Destination = new Destination
                    {
                        ToAddresses = new List<string> { itineraryEvent.LeadPassengerEmail }
                    },
                    Message = new Message
                    {
                        Subject = new Content($"Your Itinerary Confirmation: {itineraryEvent.Reference}"),
                        Body = new Body
                        {
                            Text = new Content(GenerateEmailBody(itineraryEvent))
                        }
                    }
                };
                var response = await _sesClient.SendEmailAsync(sendRequest, ct);
                context.Logger.LogInformation(
                    $"Email sent to {itineraryEvent.LeadPassengerEmail} with SES Message ID: {response.MessageId}");
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error processing message {message.MessageId}: {ex.Message}");
                throw;
            }
        }
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _sesClient.Dispose();
            }
            _disposed = true;
        }
    }
    private static string GenerateEmailBody(ItineraryCreatedEvent itinerary)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Dear Passenger,");
        sb.AppendLine();
        sb.AppendLine("Thank you for booking your flight with us. Here are the details of your itinerary:");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Reference: {itinerary.Reference}");
        sb.AppendLine();
        foreach (var booking in itinerary.Bookings)
        {
            sb.AppendLine(booking.GetSummary());
        }
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Total Price: {itinerary.TotalPrice:C2}");
        sb.AppendLine();
        sb.AppendLine("We look forward to welcoming you on board.");
        sb.AppendLine();
        sb.AppendLine("Kind regards,");
        sb.AppendLine("The Flight Booking Team");
        return sb.ToString();
    }
}
