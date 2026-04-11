import http from 'k6/http';
import { sleep } from 'k6';

/// <summary>
/// Configurações do teste de stress para a API de Filmes, aumentando gradualmente o número de usuários virtuais para identificar o ponto de falha do sistema.
/// </summary>
export const options = {
    stages: [
        { duration: '1m', target: 50 },
        { duration: '1m', target: 100 },
        { duration: '1m', target: 200 },
        { duration: '1m', target: 300 },
        { duration: '30s', target: 0 },
    ],
};

/// <summary>
/// Função principal do teste de stress, que realiza uma requisição GET para a API de Filmes, simulando um aumento gradual de carga para identificar o ponto de falha do sistema.
/// </summary>
export default function () {
    http.get('https://localhost:7277/api/Filmes', {
        insecureSkipTLSVerify: true,
    });
    sleep(1);
}