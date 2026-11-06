// Starts `ng serve` on the port Aspire assigns (PORT). npm scripts can't expand
// environment variables the same way on every OS, so this does it in Node.
import { spawn } from 'node:child_process';

const port = process.env.PORT;
if (!port) {
  console.error('PORT is not set. Run this through the Aspire AppHost, or use `npm start`.');
  process.exit(1);
}

const child = spawn('npx', ['ng', 'serve', '--port', port], { stdio: 'inherit', shell: true });
child.on('exit', (code) => process.exit(code ?? 0));
