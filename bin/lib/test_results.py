"""Unity Test Framework の結果の XML（NUnit 3 の形）から、テストごとの結果と、失敗したテストの文面を出す"""
import sys
import xml.etree.ElementTree as ET


def main(path):
    root = ET.parse(path).getroot()
    cases = root.iter('test-case')
    failed = 0
    count = 0
    for c in cases:
        count += 1
        result = c.get('result')
        print(f"{result}: {c.get('fullname')} ({float(c.get('duration', 0)):.1f}s)")
        if result != 'Passed':
            failed += 1
            msg = c.find('failure/message')
            if msg is not None and msg.text:
                print('  ' + msg.text.strip().replace('\n', '\n  '))
    print(f'テスト {count} 件、成功 {count - failed} 件、失敗 {failed} 件')
    return 1 if failed or count == 0 else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1]))
