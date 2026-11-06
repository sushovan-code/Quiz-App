// Forwards API and hub calls from `ng serve` to the server. Under Aspire the server's address comes
// from service discovery; otherwise it is the default local port.
const target = process.env['services__api__http__0'] ?? 'http://localhost:5000';

module.exports = {
  '/api': { target },
  '/openapi': { target },
  '/hubs': { target, ws: true },
};
