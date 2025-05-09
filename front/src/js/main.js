document.addEventListener("DOMContentLoaded", function() {
    // Define a data padrão para o campo de data (hoje)
    const hoje = new Date();
    const dataFormatada = hoje.toISOString().split('T')[0];
    document.getElementById('dataVencimento').value = dataFormatada;
});

function formatarData(data) {
    try {
        if (!data) return null;
        
        // Converte a string da data para objeto Date
        const dataObj = new Date(data);
        
        // Verifica se é uma data válida
        if (isNaN(dataObj.getTime())) {
            throw new Error('Data inválida');
        }
        
        // Ajusta para o fuso horário local e formata
        const dia = dataObj.getDate().toString().padStart(2, '0');
        const mes = (dataObj.getMonth() + 1).toString().padStart(2, '0');
        const ano = dataObj.getFullYear();
        
        return `${dia}/${mes}/${ano}`;
    } catch (error) {
        console.error('Erro ao formatar data:', error);
        return null;
    }
}

async function gerarCodigoBarras() {
    // Obter os valores dos campos
    const dataVencimento = document.getElementById('dataVencimento').value;
    const valor = document.getElementById('valor').value;
    
    // Validar os campos
    if (!dataVencimento || !valor) {
        alert('Por favor, preencha todos os campos.');
        return;
    }
    
    // Mostrar o loader e desabilitar o botão
    document.getElementById('loader').style.display = 'block';
    document.getElementById('resultContent').style.display = 'none';
    document.getElementById('gerarButton').disabled = true;
    
    try {
        // Chamar a API (sem formatação da data)
        const response = await fetch('http://localhost:7129/api/barcode-generate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                dataVencimento: dataVencimento,
                valor: parseFloat(valor)
            })
        });
        
        if (!response.ok) {
            throw new Error(`Erro: ${response.status}`);
        }
        
        const data = await response.json();
        
        // Exibir o código de barras
        document.getElementById('barcodeImage').src = `data:image/png;base64,${data.imagemBase64}`;
        document.getElementById('barcodeText').textContent = data.barcode;
        
        // Mostrar o resultado
        document.getElementById('resultContent').style.display = 'block';
    } catch (error) {
        console.error('Erro ao gerar o código de barras:', error);
        alert('Erro ao gerar o código de barras. Verifique o console para mais detalhes.');
    } finally {
        // Ocultar o loader e habilitar o botão
        document.getElementById('loader').style.display = 'none';
        document.getElementById('gerarButton').disabled = false;
    }
}

function formatarDataParaValidacao(dataString) {
    try {
        const data = new Date(dataString);
        const dia = data.getDate().toString().padStart(2, '0');
        const mes = (data.getMonth() + 1).toString().padStart(2, '0');
        const ano = data.getFullYear();
        return `${dia}/${mes}/${ano}`;
    } catch (error) {
        console.error('Erro ao formatar data:', error);
        return dataString;
    }
}

async function validarCodigo() {
    const barcodeText = document.getElementById('barcodeText').textContent;
    const barcodeElement = document.getElementById('barcodeText');
    
    if (!barcodeText) {
        alert('Por favor, gere um código de barras primeiro.');
        return;
    }

    try {
        // Mostrar loader e desabilitar botão
        document.getElementById('loader').style.display = 'block';
        document.getElementById('btn-validar-codigo').disabled = true;

        // Pegar a data atual para validação
        const dataAtual = formatarDataParaValidacao(new Date().toISOString());

        const response = await fetch('http://localhost:7190/api/barcode-validate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                barcode: barcodeText,
                dataAtual: dataAtual
            })
        });

        if (!response.ok) {
            throw new Error(`Erro: ${response.status}`);
        }

        const data = await response.json();
        
        // Aplicar cor baseado no resultado da validação
        if (data.valid) {
            barcodeElement.style.color = '#28a745'; // Verde para válido
            alert('Código de barras válido!');
        } else {
            barcodeElement.style.color = '#dc3545'; // Vermelho para inválido
            alert('Código de barras inválido!');
        }

    } catch (error) {
        console.error('Erro ao validar o código de barras:', error);
        alert('Erro ao validar o código de barras. Verifique o console para mais detalhes.');
    } finally {
        // Ocultar loader e habilitar botão
        document.getElementById('loader').style.display = 'none';
        document.getElementById('btn-validar-codigo').disabled = false;
    }
}