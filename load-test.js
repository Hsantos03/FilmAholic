import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
    stages: [
        { duration: '30s', target: 10 },
        { duration: '1m', target: 50 },
        { duration: '30s', target: 0 },
    ],
    thresholds: {
        http_req_duration: ['p(95)<2000'],
        http_req_failed: ['rate<0.01'],
    },
};

export default function () {
    const res = http.get('https://localhost:7277/api/Filmes', {
        insecureSkipTLSVerify: true,
    });

    check(res, {
        'status e 200': (r) => r.status === 200,
        'tempo < 2000ms': (r) => r.timings.duration < 2000,
    });

    sleep(1);
}