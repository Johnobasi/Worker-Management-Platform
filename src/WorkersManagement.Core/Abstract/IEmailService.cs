using WorkersManagement.Infrastructure.EmailComposerDtos;

namespace WorkersManagement.Core.Abstract
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
        Task<bool> SendBulkEmailAsync(BulkEmailDto emailDto);

    }

}
