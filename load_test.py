import asyncio
import concurrent.futures
import threading
import time
import pandas as pd

from kafka import KafkaConsumer
KAFKA_BOOTSTRAP_SERVERS = [
    'b-1.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092',
    'b-2.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092',
    'b-3.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092'
]


def get_kafka_consumer():
    return KafkaConsumer(bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS, group_id='bot_tests')

import requests
import os
import json

def save_curve(url:str, data:str):
    result = requests.post(url, data=data)
    print(f'{result} for posted to {url}')

class Tester(object):
    def __init__(self, DEV=True):
        self.start_time = time.time()

        if_dev = '.dev' if DEV else ''
        self.base_url = f'http://settles-api{if_dev}.mosaic.hartreepartners.com/settles/api/v1/setTraderCurve'

        if DEV:
            self.topic = 'dev-trader-curve-evals'
        else:
            self.topic = 'prod-trader-curve-evals'

        self.lock = threading.Lock()
        self.published_messages = []
        self.received_messages = []

        self.outright_template = {
            "symbol": "PERF-TEST-CURVE",
            "source": "hartree",
            "stamp": "2020-12-09",
            "definition": {
                "type": "Outright",
                "contracts": [
                    "202012",
                    "202101",
                    "202102"
                ],
                "marks": [
                    1.0,
                    2.0,
                    3.0
                ]
            },
            "epoch_time": 1609436265
        }

    def start_publisher(self):
        for i in range(200,300):
            curve = self.outright_template.copy()
            curve['definition']['marks'][0] = i
            print(f'saving mark {i}')
            time.sleep(0.5)
            save_curve(self.base_url, data=json.dumps(curve))
            # with concurrent.futures.ThreadPoolExecutor(max_workers=3) as executor:
            #     executor.submit(save_curve, url=self.base_url, data=json.dumps(curve))

    def start_consumer(self):
        with self.lock:
            self.start_time = time.time()

        kafka_consumer = KafkaConsumer(self.topic, bootstrap_servers=KAFKA_BOOTSTRAP_SERVERS, group_id=None, auto_offset_reset='earliest')
        print(self.topic)
        kafka_consumer.subscribe(topics=[self.topic])

        for raw_msg in kafka_consumer:
            # print(f'received msg :{raw_msg}')
            key = raw_msg.key.decode('utf-8')
            received_eval_curve = raw_msg.value.decode('utf-8')
            msg = json.loads(received_eval_curve)
            msg['key'] = key
            try:
                if 'curve' in msg:
                    msg['seq_num'] = msg['curve'][0]['static']
            except:
                msg['seq_num'] = -1
            if self.start_time > raw_msg.timestamp/1000:
                print(f'ignoring msg {received_eval_curve}')
            else:
                with self.lock:
                    self.received_messages.append(msg)

if __name__ == "__main__":
    tester = Tester()
    executor = concurrent.futures.ThreadPoolExecutor(max_workers=2)
    executor.submit(tester.start_consumer)
    executor.submit(tester.start_publisher)



    print('done')
    input('Enter any key to kill process')
    df = pd.DataFrame(data=tester.received_messages)
    df.to_csv(r'c:/temp/received_with_sleep.csv', index=False)
    executor.shutdown()