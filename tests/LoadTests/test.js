import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    vus: 100,
    duration: '1m',
    
};

const BASE_URL = 'http://localhost/api/audits';

export default () => {
    http.get(`${BASE_URL}`).json()
    sleep(1);
};