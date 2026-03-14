# WfWattch2Mqtt
ラトック　RS-WWATTCH2を利用したHA向けモニター

<a href="https://github.com/usausa" targert="blank">うさうさ様</a>の<a href="https://github.com/usausa/devicelib-wfwattch2" targer="blank">devicelib-wfwattch2</a>を利用した
HomeAssistantのIntegration Pluginになります。

実行イメージはこのような感じになります。
※ここでは3個のKS-WFWATTCH2からデータを取得しています。
<img width="1236" height="735" alt="Image" src="https://github.com/user-attachments/assets/b44306af-63a5-4976-aa8a-453e23137b55" />

Install方法としては、以下の手順で実施しました。<br/>
<ol>
  <li>HomeAssiostansceに.Net実行環境を用意する</li>
  <li>ビルド済みオブジェクトを対象となるHomeAssistanceに置</li>
  <li>credential.jsonを必要に応じて変更する</li>
  <li>Netが起動したら、自動的にオブジェクトが生成されるので待つ</li>
  <li>Entitiとして表示されることを確認する</li>
</li>

<p/>
注意事項としては、HomeASSISTANCEにWiFiアダプタがあった方がいいです。
