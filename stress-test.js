import http from 'k6/http';
import { sleep } from 'k6';

export const options = {
    stages: [
        { duration: '1m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '1m', target: 200 },
        { duration: '1m', target: 300 },
        { duration: '30s', target: 0 },
    ],
};

export default function () {
    http.get('https://localhost:7277/api/Filmes', {
        insecureSkipTLSVerify: true,
    });
    sleep(1);
}